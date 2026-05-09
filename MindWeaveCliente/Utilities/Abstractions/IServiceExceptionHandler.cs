using System;

namespace MindWeaveClient.Utilities.Abstractions
{
    public interface IServiceExceptionHandler
    {
        bool handleException(Exception exception, string operationContext = null);

        void handleExceptionAsync(Exception exception, string operationContext = null);

        bool handleExceptionSilent(Exception exception);
    }
}