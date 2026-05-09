using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Utilities.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Windows;
using MindWeaveClient.View.Game;
using MindWeaveClient.View.Main;

namespace MindWeaveClient.Utilities.Implementations
{
    public class ServiceExceptionHandler : IServiceExceptionHandler
    {
        private const int WSAE_NET_DOWN = 10050;
        private const int WSAE_NET_UNREACH = 10051;
        private const int WSAE_NET_RESET = 10052;
        private const int WSAE_CONN_ABORTED = 10053;
        private const int WSAE_CONN_RESET = 10054;
        private const int WSAE_NOT_CONN = 10057;
        private const int WSAE_HOST_DOWN = 10064;
        private const int WSAE_HOST_UNREACH = 10065;
        private const int WSA_SYS_NOT_READY = 10091;
        private const int WSA_NOT_INITIALISED = 10093;

        private const string KEYWORD_CONNECTION = "connection";
        private const string KEYWORD_SOCKET = "socket";
        private const string KEYWORD_REFUSED = "refused";
        private const string KEYWORD_RESET = "reset";
        private const string KEYWORD_NETWORK = "network";
        private const string KEYWORD_UNREACHABLE = "unreachable";
        private const string KEYWORD_CHANNEL = "channel";
        private const string KEYWORD_FAULTED = "faulted";
        private const string KEYWORD_CLOSED = "closed";
        private const string KEYWORD_ABORTED = "aborted";
        private const string KEYWORD_CANNOT_BE_USED = "cannot be used";
        private const string KEYWORD_COMMUNICATION_OBJECT = "communication object";
        private const string KEYWORD_STATE = "state";

        private readonly IDialogService dialogService;
        private readonly IWindowNavigationService windowNavigationService;
        private readonly Lazy<ISessionCleanupService> sessionCleanupServiceLazy;

        private bool isHandlingCriticalError;
        private readonly object lockObject = new object();

        public ServiceExceptionHandler(
            IDialogService dialogService,
            IWindowNavigationService windowNavigationService,
            Lazy<ISessionCleanupService> sessionCleanupServiceLazy)
        {
            this.dialogService = dialogService;
            this.windowNavigationService = windowNavigationService;
            this.sessionCleanupServiceLazy = sessionCleanupServiceLazy;
        }

        public bool handleException(Exception exception, string operationContext = null)
        {
            if (exception == null) return false;

            lock (lockObject)
            {
                if (isHandlingCriticalError) return true;
            }

            if (exception is FaultException faultException)
            {
                handleFaultException(faultException);
                return false;
            }

            if (isChannelStateError(exception))
            {
                handleCriticalConnectionError(exception);
                return true;
            }

            if (isNetworkUnavailableError(exception))
            {
                handleNetworkUnavailableError(exception);
                return true;
            }

            if (isCriticalConnectionError(exception))
            {
                handleCriticalConnectionError(exception);
                return true;
            }

            string message = getExceptionMessage(exception, operationContext);
            showErrorOnUIThread(message, Lang.ErrorTitle);
            return false;
        }

        public void handleExceptionAsync(Exception exception, string operationContext = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                handleException(exception, operationContext);
            });
        }

        public bool handleExceptionSilent(Exception exception)
        {
            if (exception == null) return false;

            bool isCritical = isChannelStateError(exception) ||
                              isNetworkUnavailableError(exception) ||
                              isCriticalConnectionError(exception);

            if (!isCritical)
            {
                return false;
            }

            lock (lockObject)
            {
                if (isHandlingCriticalError)
                {
                    return true;
                }
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                handleException(exception);
            }));

            return true;
        }

        private void handleNetworkUnavailableError(Exception exception)
        {
            lock (lockObject)
            {
                if (isHandlingCriticalError) return;
                isHandlingCriticalError = true;
            }

            string message = getNetworkUnavailableMessage(exception);
            showErrorOnUIThread(message, Lang.ErrorNoInternetTitle);

            executeResetSequence();
        }

        private void handleCriticalConnectionError(Exception exception)
        {
            lock (lockObject)
            {
                if (isHandlingCriticalError) return;
                isHandlingCriticalError = true;
            }

            string message = getCriticalErrorMessage(exception);
            showErrorOnUIThread(message, Lang.ErrorConnectionLostTitle);

            executeResetSequence();
        }

        private void executeResetSequence()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        if (sessionCleanupServiceLazy != null && sessionCleanupServiceLazy.Value != null)
                        {
                            await sessionCleanupServiceLazy.Value.cleanUpSessionAsync();
                        }

                        resetApplicationWindows();
                    }
                    catch (Exception)
                    {
                        /*
                         * Ignore: Error handling during a reset sequence is a "best effort" operation.
                         * If cleanup or window navigation fails while recovering from a critical error,
                         * suppressing the exception prevents a crash loop and attempts to leave the
                         * application in the most stable state possible (usually the login screen).
                         */
                    }
                    finally
                    {
                        lock (lockObject)
                        {
                            isHandlingCriticalError = false;
                        }
                    }
                });
            }
            catch (Exception)
            {
                lock (lockObject)
                {
                    isHandlingCriticalError = false;
                }
            }
        }

        public bool isChannelStateError(Exception exception)
        {
            if (exception == null) return false;

            if (exception is InvalidOperationException invalidOpEx)
            {
                string message = invalidOpEx.Message.ToLowerInvariant();

                if (message.Contains(KEYWORD_CHANNEL) ||
                    message.Contains(KEYWORD_FAULTED) ||
                    message.Contains(KEYWORD_CLOSED) ||
                    message.Contains(KEYWORD_ABORTED) ||
                    message.Contains(KEYWORD_CANNOT_BE_USED) ||
                    message.Contains(KEYWORD_COMMUNICATION_OBJECT) ||
                    (message.Contains(KEYWORD_STATE) && message.Contains(KEYWORD_FAULTED)))
                {
                    return true;
                }
            }

            if (exception is ObjectDisposedException)
            {
                return true;
            }

            if (exception.InnerException != null)
            {
                return isChannelStateError(exception.InnerException);
            }

            return false;
        }

        public bool isCriticalConnectionError(Exception exception)
        {
            if (exception == null) return false;

            if (isCriticalExceptionType(exception)) return true;

            if (exception is CommunicationException commEx && isCriticalCommunicationException(commEx))
            {
                return true;
            }

            if (exception is InvalidOperationException invalidOpEx && isCriticalInvalidOperationException(invalidOpEx))
            {
                return true;
            }

            return exception.InnerException != null && isCriticalConnectionError(exception.InnerException);
        }

        private static bool isCriticalExceptionType(Exception exception)
        {
            return exception is EndpointNotFoundException
                || exception is CommunicationObjectFaultedException
                || exception is CommunicationObjectAbortedException
                || exception is ChannelTerminatedException
                || exception is ServerTooBusyException
                || exception is ObjectDisposedException
                || exception is TimeoutException
                || exception is SocketException
                || exception is WebException;
        }

        private static bool isCriticalCommunicationException(CommunicationException commEx)
        {
            if (hasCriticalInnerException(commEx.InnerException))
            {
                return true;
            }

            return containsCriticalConnectionKeywords(commEx.Message);
        }

        private static bool hasCriticalInnerException(Exception innerException)
        {
            return innerException is SocketException
                || innerException is WebException
                || innerException is ObjectDisposedException;
        }

        private static bool containsCriticalConnectionKeywords(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            string lowerMessage = message.ToLowerInvariant();

            return lowerMessage.Contains(KEYWORD_CONNECTION)
                || lowerMessage.Contains(KEYWORD_SOCKET)
                || lowerMessage.Contains(KEYWORD_REFUSED)
                || lowerMessage.Contains(KEYWORD_RESET)
                || lowerMessage.Contains(KEYWORD_FAULTED)
                || lowerMessage.Contains(KEYWORD_ABORTED)
                || lowerMessage.Contains(KEYWORD_CLOSED);
        }

        private static bool isCriticalInvalidOperationException(InvalidOperationException invalidOpEx)
        {
            string message = invalidOpEx.Message.ToLowerInvariant();

            return message.Contains(KEYWORD_CHANNEL)
                || message.Contains(KEYWORD_COMMUNICATION_OBJECT)
                || message.Contains(KEYWORD_FAULTED)
                || message.Contains(KEYWORD_CLOSED);
        }

        public bool isNetworkUnavailableError(Exception exception)
        {
            if (exception == null) return false;

            if (exception is SocketException socketEx &&
                (isNetworkDownSocketError(socketEx.SocketErrorCode) || isNetworkDownErrorCode(socketEx.ErrorCode)))
            {
                return true;
            }

            if (exception is WebException webEx &&
                isNetworkRelatedWebException(webEx))
            {
                return true;
            }

            if (exception is CommunicationException commEx &&
                isNetworkRelatedCommunicationException(commEx))
            {
                return true;
            }

            if (exception is EndpointNotFoundException && !isNetworkAvailable())
            {
                return true;
            }

            return exception.InnerException != null && isNetworkUnavailableError(exception.InnerException);
        }

        private static bool isNetworkRelatedWebException(WebException webEx)
        {
            bool hasNetworkFailureStatus = webEx.Status == WebExceptionStatus.NameResolutionFailure ||
                                           webEx.Status == WebExceptionStatus.ProxyNameResolutionFailure ||
                                           webEx.Status == WebExceptionStatus.ConnectFailure;

            return hasNetworkFailureStatus && !isNetworkAvailable();
        }

        private static bool isNetworkRelatedCommunicationException(CommunicationException commEx)
        {
            string message = commEx.Message.ToLowerInvariant();
            if (message.Contains(KEYWORD_NETWORK) && message.Contains(KEYWORD_UNREACHABLE))
            {
                return true;
            }

            if (commEx.InnerException is SocketException innerSocket &&
                (isNetworkDownSocketError(innerSocket.SocketErrorCode) || isNetworkDownErrorCode(innerSocket.ErrorCode)))
            {
                return true;
            }

            if (commEx.InnerException is WebException innerWeb &&
                isNetworkRelatedWebException(innerWeb))
            {
                return true;
            }

            return false;
        }


        private static bool isNetworkDownSocketError(SocketError errorCode)
        {
            switch (errorCode)
            {
                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:
                case SocketError.NetworkReset:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.NotConnected:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                case SocketError.SystemNotReady:
                case SocketError.NotInitialized:
                    return true;
                default:
                    return false;
            }
        }

        private static bool isNetworkDownErrorCode(int errorCode)
        {
            switch (errorCode)
            {
                case WSAE_NET_DOWN:
                case WSAE_NET_UNREACH:
                case WSAE_NET_RESET:
                case WSAE_CONN_ABORTED:
                case WSAE_CONN_RESET:
                case WSAE_NOT_CONN:
                case WSAE_HOST_DOWN:
                case WSAE_HOST_UNREACH:
                case WSA_SYS_NOT_READY:
                case WSA_NOT_INITIALISED:
                    return true;
                default:
                    return false;
            }
        }

        private static bool isNetworkAvailable()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return false;
                }

                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var netInterface in interfaces)
                {
                    if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        netInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    if (netInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        var ipProperties = netInterface.GetIPProperties();
                        if (ipProperties.UnicastAddresses.Count > 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        private void handleFaultException(FaultException faultException)
        {
            string messageCode = null;
            var detailProperty = faultException.GetType().GetProperty("Detail");
            if (detailProperty != null)
            {
                var detailValue = detailProperty.GetValue(faultException);
                if (detailValue != null)
                {
                    var detailType = detailValue.GetType();

                    var codeProperty = detailType.GetProperty("MessageCode");
                    if (codeProperty != null)
                    {
                        messageCode = codeProperty.GetValue(detailValue)?.ToString();
                    }
                }
            }

            var localizedMessage = !string.IsNullOrEmpty(messageCode) ? MessageCodeInterpreter.translate(messageCode) : faultException.Message;

            showErrorOnUIThread(localizedMessage, Lang.ErrorTitle);
        }

        private static string getNetworkUnavailableMessage(Exception exception)
        {
            SocketException socketEx = findSocketException(exception);
            if (socketEx != null)
            {
                switch (socketEx.SocketErrorCode)
                {
                    case SocketError.NetworkDown:
                        return Lang.ErrorNetworkDown;
                    case SocketError.NetworkUnreachable:
                    case SocketError.HostUnreachable:
                        return Lang.ErrorNetworkUnreachable;
                    case SocketError.NotConnected:
                        return Lang.ErrorNotConnectedToNetwork;
                    case SocketError.HostDown:
                        return Lang.ErrorHostDown;
                    case SocketError.SystemNotReady:
                    case SocketError.NotInitialized:
                        return Lang.ErrorNetworkNotReady;
                }
            }

            WebException webEx = findWebException(exception);
            if (webEx != null)
            {
                switch (webEx.Status)
                {
                    case WebExceptionStatus.NameResolutionFailure:
                        return Lang.ErrorDnsResolutionFailed;
                    case WebExceptionStatus.ConnectFailure:
                        return Lang.ErrorCannotConnectToNetwork;
                }
            }

            return Lang.ErrorNoInternetConnection;
        }

        private static string getCriticalErrorMessage(Exception exception)
        {
            if (exception is InvalidOperationException ||
                exception is CommunicationObjectFaultedException ||
                exception is CommunicationObjectAbortedException)
            {
                return Lang.ErrorServerWentDown;
            }

            if (exception is EndpointNotFoundException)
            {
                return Lang.ErrorServiceNotFound;
            }

            if (exception is TimeoutException)
            {
                return Lang.ErrorServerTimeout;
            }

            if (exception is ChannelTerminatedException)
            {
                return Lang.ErrorConnectionTerminated;
            }

            if (exception is ServerTooBusyException)
            {
                return Lang.ErrorServerBusy;
            }

            if (exception is CommunicationException commEx)
            {
                string message = commEx.Message.ToLowerInvariant();
                if (message.Contains(KEYWORD_FAULTED) || message.Contains(KEYWORD_ABORTED))
                {
                    return Lang.ErrorServerWentDown;
                }

                if (commEx.InnerException is SocketException socketEx)
                {
                    return getSocketErrorMessage(socketEx);
                }

                return Lang.ErrorConnectionLost;
            }

            if (exception is SocketException socketException)
            {
                return getSocketErrorMessage(socketException);
            }

            return Lang.ErrorConnectionLost;
        }

        private static string getSocketErrorMessage(SocketException socketException)
        {
            switch (socketException.SocketErrorCode)
            {
                case SocketError.ConnectionRefused:
                    return Lang.ErrorConnectionRefused;
                case SocketError.ConnectionReset:
                    return Lang.ErrorConnectionReset;
                case SocketError.HostUnreachable:
                case SocketError.NetworkUnreachable:
                    return Lang.ErrorNetworkUnreachable;
                case SocketError.HostNotFound:
                    return Lang.ErrorHostNotFound;
                case SocketError.TimedOut:
                    return Lang.ErrorServerTimeout;
                case SocketError.NetworkDown:
                    return Lang.ErrorNetworkDown;
                case SocketError.NotConnected:
                    return Lang.ErrorNotConnectedToNetwork;
                default:
                    return Lang.ErrorConnectionLost;
            }
        }

        private static string getExceptionMessage(Exception exception, string operationContext)
        {
            if (exception is SecurityNegotiationException)
                return Lang.ErrorSecurityNegotiation;

            if (exception is MessageSecurityException)
                return Lang.ErrorMessageSecurity;

            if (exception is QuotaExceededException)
                return Lang.ErrorQuotaExceeded;

            if (exception is ProtocolException)
                return Lang.ErrorProtocol;

            if (exception is ActionNotSupportedException)
                return Lang.ErrorActionNotSupported;

            if (exception is InvalidOperationException)
                return Lang.ErrorInvalidOperation;

            if (exception is ArgumentException)
                return Lang.ErrorInvalidData;

            return string.Format(Lang.ErrorGenericOperation, operationContext ?? Lang.GlobalLbUnknown);
        }

        private static SocketException findSocketException(Exception exception)
        {
            if (exception is SocketException socketEx)
                return socketEx;

            if (exception.InnerException != null)
                return findSocketException(exception.InnerException);

            return null;
        }

        private static WebException findWebException(Exception exception)
        {
            if (exception is WebException webEx)
                return webEx;

            if (exception.InnerException != null)
                return findWebException(exception.InnerException);

            return null;
        }

        private void showErrorOnUIThread(string message, string title)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                dialogService.showError(message, title);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    dialogService.showError(message, title);
                });
            }
        }

        private void resetApplicationWindows()
        {
            var loginWindow = getOrCreateLoginWindow();
            if (loginWindow == null) return;

            Application.Current.MainWindow = loginWindow;
            closeWindowsExcept(loginWindow);
        }

        private Window getOrCreateLoginWindow()
        {
            var existingLogin = Application.Current.Windows
                .OfType<View.Authentication.AuthenticationWindow>()
                .FirstOrDefault();

            if (existingLogin != null)
            {
                if (existingLogin.WindowState == WindowState.Minimized)
                {
                    existingLogin.WindowState = WindowState.Normal;
                }
                existingLogin.Activate();
                return existingLogin;
            }

            windowNavigationService.openWindow<View.Authentication.AuthenticationWindow>();

            return Application.Current.Windows
                .OfType<View.Authentication.AuthenticationWindow>()
                .LastOrDefault();
        }

        private static void closeWindowsExcept(Window windowToKeep)
        {
            var windowsToClose = Application.Current.Windows
                .Cast<Window>()
                .Where(w => !ReferenceEquals(w, windowToKeep))
                .ToList();

            foreach (var window in windowsToClose)
            {
                if (window is MainWindow mainWindow)
                {
                    mainWindow.IsExitConfirmed = true;
                }
                else if (window is GameWindow gameWindow)
                {
                    gameWindow.IsExitConfirmed = true;
                }

                window.Close();
            }
        }
    }
}