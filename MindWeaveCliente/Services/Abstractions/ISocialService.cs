using MindWeaveClient.SocialManagerService;
using System;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Abstractions
{
    public interface ISocialService: IDisposable
    {
        event Action<string, bool> FriendStatusChanged;
        event Action<string> FriendRequestReceived;
        event Action<string, bool> FriendResponseReceived;
        event Action<string, string> LobbyInviteReceived;

        Task connectAsync(string username);

        Task disconnectAsync(string username, bool forceAbort = false);

        Task<FriendDto[]> getFriendsListAsync(string username);

        Task<FriendRequestInfoDto[]> getFriendRequestsAsync(string username);

        Task<PlayerSearchResultDto[]> searchPlayersAsync(string username, string query);

        Task<OperationResultDto> sendFriendRequestAsync(string username, string targetUsername);

        Task<OperationResultDto> respondToFriendRequestAsync(string username, string requesterUsername, bool accept);

        Task<OperationResultDto> removeFriendAsync(string username, string friendUsername);
    }
}