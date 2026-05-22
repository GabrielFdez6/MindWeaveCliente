using MindWeaveClient.Services.Abstractions;
using System;
using System.Collections.Generic;

namespace MindWeaveClient.Services.Implementations
{
    public class CurrentMatchService : ICurrentMatchService
    {
        private string lobbyId;
        private List<string> players;
        private PuzzleManagerService.PuzzleDefinitionDto currentPuzzle;

        public event EventHandler OnMatchFound;
        public event Action PuzzleReady;

        public Guid MatchId { get; set; }
        public string LobbyId => lobbyId;
        public List<string> Players => players;

        public void initializeMatch(MatchFoundDto matchData)
        {
            this.lobbyId = matchData.LobbyCode;
            this.players = matchData.Players;
            OnMatchFound?.Invoke(this, EventArgs.Empty);
        }
        public PuzzleManagerService.PuzzleDefinitionDto getCurrentPuzzle()
        {
            return currentPuzzle;
        }

        public void setPuzzle(PuzzleManagerService.PuzzleDefinitionDto puzzle)
        {
            this.currentPuzzle = puzzle;
            PuzzleReady?.Invoke();
        }

        public void clearMatchData()
        {
            lobbyId = null;
            players = null;
            currentPuzzle = null;
            MatchId = Guid.Empty;
        }
    }
}