using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.SocialManagerService;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.Utilities.Implementations;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MindWeaveClient.ViewModel.Main
{
    public class FriendDtoDisplay : BaseViewModel
    {
        private const string DEFAULT_AVATAR_PATH = "/Resources/Images/Avatar/default_avatar.png";
        private bool isOnlineValue;

        public string Username { get; set; }
        public string AvatarPath { get; set; }

        public bool IsOnline
        {
            get => isOnlineValue;
            set { isOnlineValue = value; OnPropertyChanged(); }
        }

        public FriendDtoDisplay(FriendDto dto)
        {
            this.Username = dto.Username;
            this.AvatarPath = dto.AvatarPath ?? DEFAULT_AVATAR_PATH;
            this.IsOnline = dto.IsOnline;
        }

        public FriendDtoDisplay() { }
    }

    public class SocialViewModel : BaseViewModel
    {
        private const int MAX_SEARCH_LENGTH = 16;
        private readonly IDialogService dialogService;
        private readonly ISocialService socialService;
        private readonly IServiceExceptionHandler exceptionHandler;

        private string searchQueryValue;
        private bool isFriendsListCheckedValue = true;
        private bool isAddFriendCheckedValue;
        private bool isRequestsCheckedValue;
        private readonly string currentUserUsername;

        public ObservableCollection<FriendDtoDisplay> FriendsList { get; } = new ObservableCollection<FriendDtoDisplay>();
        public ObservableCollection<PlayerSearchResultDto> SearchResults { get; } = new ObservableCollection<PlayerSearchResultDto>();
        public ObservableCollection<FriendRequestInfoDto> ReceivedRequests { get; } = new ObservableCollection<FriendRequestInfoDto>();

        

        public string SearchQuery
        {
            get => searchQueryValue;
            set
            {
                string processedValue = clampString(value, MAX_SEARCH_LENGTH);

                if (searchQueryValue != processedValue)
                {
                    searchQueryValue = processedValue;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFriendsListChecked
        {
            get => isFriendsListCheckedValue;
            set
            {
                isFriendsListCheckedValue = value;
                OnPropertyChanged();
                if (value) LoadFriendsListCommand.Execute(null);
            }
        }

        public bool IsAddFriendChecked
        {
            get => isAddFriendCheckedValue;
            set
            {
                isAddFriendCheckedValue = value;
                OnPropertyChanged();
                if (value) { SearchResults.Clear(); SearchQuery = ""; }
            }
        }

        public bool IsRequestsChecked
        {
            get => isRequestsCheckedValue;
            set
            {
                isRequestsCheckedValue = value;
                OnPropertyChanged();
                if (value) LoadRequestsCommand.Execute(null);
            }
        }

        public ICommand LoadFriendsListCommand { get; }
        public ICommand LoadRequestsCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SendRequestCommand { get; }
        public ICommand AcceptRequestCommand { get; }
        public ICommand DeclineRequestCommand { get; }
        public ICommand RemoveFriendCommand { get; }
        public ICommand BackCommand { get; }

        public SocialViewModel(
            INavigationService navigationService,
            IDialogService dialogService,
            ISocialService socialService,
            IServiceExceptionHandler exceptionHandler)
        {
            var navigationService1 = navigationService;
            this.dialogService = dialogService;
            this.socialService = socialService;
            this.exceptionHandler = exceptionHandler;

            currentUserUsername = SessionService.Username;

            LoadFriendsListCommand = new RelayCommand(async (param) => await executeLoadFriendsListAsync(), (param) => !IsBusy);
            LoadRequestsCommand = new RelayCommand(async (param) => await executeLoadRequestsAsync(), (param) => !IsBusy);
            SearchCommand = new RelayCommand(async (param) => await executeSearchAsync(), (param) => !IsBusy && !string.IsNullOrWhiteSpace(SearchQuery));
            SendRequestCommand = new RelayCommand(async (param) => await executeSendRequestAsync(param as PlayerSearchResultDto), (param) => !IsBusy && param is PlayerSearchResultDto);
            AcceptRequestCommand = new RelayCommand(async (param) => await executeRespondRequestAsync(param as FriendRequestInfoDto, true), (param) => !IsBusy && param is FriendRequestInfoDto);
            DeclineRequestCommand = new RelayCommand(async (param) => await executeRespondRequestAsync(param as FriendRequestInfoDto, false), (param) => !IsBusy && param is FriendRequestInfoDto);
            RemoveFriendCommand = new RelayCommand(async (param) => await executeRemoveFriendAsync(param as FriendDtoDisplay), (param) => !IsBusy && param is FriendDtoDisplay);
            BackCommand = new RelayCommand((param) => navigationService1.goBack(), (param) => !IsBusy);

            subscribeToEvents();

            if (IsFriendsListChecked)
            {
                LoadFriendsListCommand.Execute(null);
            }
            else if (IsRequestsChecked)
            {
                LoadRequestsCommand.Execute(null);
            }
        }

        private static string clampString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private void subscribeToEvents()
        {
            socialService.FriendRequestReceived += handleFriendRequestReceived;
            socialService.FriendResponseReceived += handleFriendResponseReceived;
            socialService.FriendStatusChanged += handleFriendStatusChanged;
        }

        private void unsubscribeFromEvents()
        {
            socialService.FriendRequestReceived -= handleFriendRequestReceived;
            socialService.FriendResponseReceived -= handleFriendResponseReceived;
            socialService.FriendStatusChanged -= handleFriendStatusChanged;
        }

        private async Task executeLoadFriendsListAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            FriendsList.Clear();
            try
            {
                FriendDto[] friends = await socialService.getFriendsListAsync(currentUserUsername);
                if (friends != null)
                {
                    foreach (var friendDto in friends)
                    {
                        FriendsList.Add(new FriendDtoDisplay(friendDto));
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.LoadFriendsOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task executeLoadRequestsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ReceivedRequests.Clear();
            try
            {
                FriendRequestInfoDto[] requests = await socialService.getFriendRequestsAsync(currentUserUsername);
                if (requests != null)
                {
                    foreach (var req in requests) ReceivedRequests.Add(req);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.LoadRequestsOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task executeSearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            SearchResults.Clear();
            try
            {
                PlayerSearchResultDto[] results = await socialService.searchPlayersAsync(currentUserUsername, SearchQuery);
                if (results != null)
                {
                    foreach (var user in results) SearchResults.Add(user);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.SearchPlayersOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task executeSendRequestAsync(PlayerSearchResultDto targetUser)
        {
            if (targetUser == null || IsBusy) return;
            IsBusy = true;
            try
            {
                OperationResultDto result = await socialService.sendFriendRequestAsync(currentUserUsername, targetUser.Username);

                if (result.Success)
                {
                    string successMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showInfo(successMsg, Lang.InfoMsgTitleSuccess);
                    SearchResults.Remove(targetUser);
                }
                else
                {
                    string errorMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showWarning(errorMsg, Lang.WarningTitle);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.SendFriendRequestOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task executeRespondRequestAsync(FriendRequestInfoDto request, bool accept)
        {
            if (request == null || IsBusy) return;
            IsBusy = true;
            try
            {
                OperationResultDto result = await socialService.respondToFriendRequestAsync(currentUserUsername, request.RequesterUsername, accept);

                if (result.Success)
                {
                    string successMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showInfo(successMsg, Lang.InfoMsgTitleSuccess);
                    ReceivedRequests.Remove(request);
                    if (accept && IsFriendsListChecked)
                    {
                        await executeLoadFriendsListAsync();
                    }
                }
                else
                {
                    string errorMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showError(errorMsg, Lang.ErrorTitle);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.RespondFriendRequestOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task executeRemoveFriendAsync(FriendDtoDisplay friendToRemove)
        {
            if (friendToRemove == null || IsBusy) return;

            bool confirmResult = dialogService.showConfirmation(
                string.Format(Lang.SocialConfirmRemove, friendToRemove.Username),
                Lang.SocialConfirmRemoveTitle);

            if (!confirmResult) return;

            IsBusy = true;
            try
            {
                OperationResultDto result = await socialService.removeFriendAsync(currentUserUsername, friendToRemove.Username);

                if (result.Success)
                {
                    string successMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showInfo(successMsg, Lang.InfoMsgTitleSuccess);
                    FriendsList.Remove(friendToRemove);
                }
                else
                {
                    string errorMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showError(errorMsg, Lang.ErrorTitle);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.RemoveFriendOperation);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Event Handlers
        private void handleFriendRequestReceived(string fromUsername)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if (IsRequestsChecked)
                {
                    await executeLoadRequestsAsync();
                }
                dialogService.showInfo(
                    string.Format(Lang.SocialInfoNewRequest, fromUsername),
                    Lang.SocialInfoNewRequestTitle);
            });
        }

        private void handleFriendResponseReceived(string fromUsername, bool accepted)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if (accepted)
                {
                    if (IsFriendsListChecked)
                    {
                        await executeLoadFriendsListAsync();
                    }
                    dialogService.showInfo(
                        string.Format(Lang.SocialInfoRequestAccepted, fromUsername),
                        Lang.SocialInfoNewFriendTitle);
                }
                else
                {
                    dialogService.showInfo(
                        string.Format(Lang.SocialInfoRequestDeclined, fromUsername),
                        Lang.SocialInfoRequestDeclinedTitle);
                }
            });
        }

        private void handleFriendStatusChanged(string friendUsername, bool isOnline)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var friend = FriendsList.FirstOrDefault(f =>
                    f.Username.Equals(friendUsername, StringComparison.OrdinalIgnoreCase));

                if (friend != null)
                {
                    friend.IsOnline = isOnline;
                }
            });
        }

        public void cleanup()
        {
            unsubscribeFromEvents();
        }
    }
}