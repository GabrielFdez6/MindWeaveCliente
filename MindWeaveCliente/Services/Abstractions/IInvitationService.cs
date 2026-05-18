namespace MindWeaveClient.Services.Abstractions
{
    public interface IInvitationService
    {
        void subscribeToGlobalInvites();

        void unsubscribeFromGlobalInvites();
    }
}
