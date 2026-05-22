using MindWeaveClient.MatchmakingService;
using System;
using System.Collections.Generic;

namespace MindWeaveClient.Services.Abstractions
{
    public interface ICurrentMatchService
    {
        string LobbyId { get; }
        List<string> Players { get; }

        event Action PuzzleReady;

        void initializeMatch(MatchFoundDto matchData);
        PuzzleManagerService.PuzzleDefinitionDto getCurrentPuzzle();
        void setPuzzle(PuzzleManagerService.PuzzleDefinitionDto puzzle);
        void clearMatchData();
    }
}