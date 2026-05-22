using MindWeaveClient.ChatManagerService;
using System;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Abstractions
{
    public interface IChatService
    {
        event Action<ChatMessageDto> OnMessageReceived;

        bool isConnected();

        event Action<string> OnSystemMessageReceived;

        Task connectAsync(string username, string lobbyId);

        Task disconnectAsync(string username, string lobbyId, bool forceAbort = false);

        Task sendLobbyMessageAsync(string username, string lobbyId, string message);
    }
}