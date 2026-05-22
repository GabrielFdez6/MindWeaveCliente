using MindWeaveClient.MatchmakingService;

namespace MindWeaveClient.Services
{
    public class JoinLobbyResultDto
    {
        public bool Success { get; set; }
        public string MessageCode { get; set; }
        public LobbyStateDto InitialLobbyState { get; set; }
    }

}
