using System.Collections.Generic;
using MindWeaveClient.MatchmakingService;

namespace MindWeaveClient.Services
{
    public class PieceMovementDto
    {
        public int PieceId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class MatchFoundDto
    {
        public string LobbyCode { get; set; }
        public List<string> Players { get; set; }
        public LobbySettingsDto Settings { get; set; }
        public string PuzzleImagePath { get; set; }
    }
}