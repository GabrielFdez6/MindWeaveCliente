using MindWeaveClient.MatchmakingService;

namespace MindWeaveClient.Services
{
    public class GuestJoinServiceResultDto
    {
        public GuestJoinResultDto WcfResult { get; }
        public bool DidMatchmakingConnect { get; }

        public GuestJoinServiceResultDto(GuestJoinResultDto wcfResult, bool matchmakingConnected = true)
        {
            this.WcfResult = wcfResult;
            this.DidMatchmakingConnect = matchmakingConnected;
        }
    }
}
