using MindWeaveClient.MatchmakingService;

namespace MindWeaveClient.Services.Abstractions
{
    public interface ICurrentLobbyService
    {
        void setInitialState(LobbyStateDto initialState);

        LobbyStateDto getInitialState();

    
    }
}
