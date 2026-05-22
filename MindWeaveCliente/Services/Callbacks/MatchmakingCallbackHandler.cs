using MindWeaveClient.MatchmakingService;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace MindWeaveClient.Services.Callbacks
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class MatchmakingCallbackHandler : IMatchmakingManagerCallback
    {
        private readonly ICurrentMatchService currentMatchService;

        public event Action<LobbyStateDto> OnLobbyStateUpdatedEvent;
        public event Action<string, List<string>, LobbySettingsDto, string> OnMatchFoundEvent;
        public event Action<string> OnLobbyCreationFailedEvent;
        public event Action<string> OnLobbyActionFailedEvent;
        public event Action<string> OnKickedEvent;
        public event Action<string> OnLobbyDestroyedEvent;

        public static event Action<string> OnPlayerLeftEvent;
        public static event Action<int, string> PieceDragStartedHandler;
        public static event Action<int, double, double, string, int, string> PiecePlacedHandler;
        public static event Action<int, double, double, string> PieceMovedHandler;
        public static event Action<int, string> PieceDragReleasedHandler;
        public static event Action<string, int, int, string> PlayerPenaltyHandler;

        public event Action<PuzzleDefinitionDto, int> OnGameStartedNavigation;
        public static event Action<MatchEndResultDto> GameEndedStatic;
        public static MatchEndResultDto LastMatchResults { get; private set; }
        public static int LastMatchDuration { get; private set; } = 300;

        public MatchmakingCallbackHandler(
            ICurrentMatchService currentMatchService,
            IDialogService dialogService)
        {
            this.currentMatchService = currentMatchService;
        }


        public void kickedFromLobby(string reason)
        {
            OnKickedEvent?.Invoke(reason);
        }

        public void lobbyCreationFailed(string reason)
        {
            OnLobbyCreationFailedEvent?.Invoke(reason);
        }

        public void notifyLobbyActionFailed(string message)
        {
            OnLobbyActionFailedEvent?.Invoke(message);
        }

        public void matchFound(string lobbyCode, string[] players, LobbySettingsDto settings, string puzzleImagePath)
        {
            List<string> playerList = players?.ToList() ?? new List<string>();

            if (currentMatchService != null)
            {
                var matchData = new MatchFoundDto
                {
                    LobbyCode = lobbyCode,
                    Players = playerList,
                    Settings = settings,
                    PuzzleImagePath = puzzleImagePath
                };

                currentMatchService.initializeMatch(matchData);
            }

            OnMatchFoundEvent?.Invoke(lobbyCode, playerList, settings, puzzleImagePath);
        }

        public void onGameStarted(PuzzleDefinitionDto puzzleDefinition, int matchDurationSeconds)
        {
            LastMatchDuration = matchDurationSeconds;

            if (puzzleDefinition == null || currentMatchService == null) { return; }

            var puzzleManagerDto = new MindWeaveClient.PuzzleManagerService.PuzzleDefinitionDto
            {
                FullImageBytes = puzzleDefinition.FullImageBytes,
                PuzzleHeight = puzzleDefinition.PuzzleHeight,
                PuzzleWidth = puzzleDefinition.PuzzleWidth,
                SilhouetteImageBytes = puzzleDefinition.SilhouetteImageBytes,

                Pieces = puzzleDefinition.Pieces.Select(piece =>
                    new MindWeaveClient.PuzzleManagerService.PuzzlePieceDefinitionDto
                    {
                        PieceId = piece.PieceId,
                        SourceX = piece.SourceX,
                        SourceY = piece.SourceY,
                        Width = piece.Width,
                        Height = piece.Height,
                        CorrectX = piece.CorrectX,
                        CorrectY = piece.CorrectY,
                        InitialX = piece.InitialX,
                        InitialY = piece.InitialY,
                        TopNeighborId = piece.TopNeighborId,
                        BottomNeighborId = piece.BottomNeighborId,
                        LeftNeighborId = piece.LeftNeighborId,
                        RightNeighborId = piece.RightNeighborId,
                        PieceImageBytes = piece.PieceImageBytes,
                        RenderWidth = piece.RenderWidth,
                        RenderHeight = piece.RenderHeight,
                        OffsetX = piece.OffsetX,
                        OffsetY = piece.OffsetY
                    }).ToArray()
            };

            currentMatchService?.setPuzzle(puzzleManagerDto);
            OnGameStartedNavigation?.Invoke(puzzleDefinition, matchDurationSeconds);
        }

        public void updateLobbyState(LobbyStateDto lobbyStateDto)
        {
            if (currentMatchService != null && lobbyStateDto != null)
            {
                var playerList = lobbyStateDto.Players?.ToList() ?? new List<string>();
                var matchData = new MatchFoundDto
                {
                    LobbyCode = lobbyStateDto.LobbyId,
                    Players = playerList,
                    Settings = lobbyStateDto.CurrentSettingsDto,
                    PuzzleImagePath = lobbyStateDto.PuzzleImagePath
                };

                currentMatchService.initializeMatch(matchData);
            }

            OnLobbyStateUpdatedEvent?.Invoke(lobbyStateDto);
        }

        public void onPieceDragStarted(int pieceId, string username)
        {
            PieceDragStartedHandler?.Invoke(pieceId, username);
        }

        public void onPiecePlaced(int pieceId, double correctX, double correctY, string username, int newScore, string bonusType)
        {
            PiecePlacedHandler?.Invoke(pieceId, correctX, correctY, username, newScore, bonusType);
        }

        public void onPieceMoved(int pieceId, double newX, double newY, string username)
        {
            PieceMovedHandler?.Invoke(pieceId, newX, newY, username);
        }

        public void onPieceDragReleased(int pieceId, string username)
        {
            PieceDragReleasedHandler?.Invoke(pieceId, username);
        }
        public void onPlayerPenalty(string username, int pointsLost, int newScore, string reason)
        {
            PlayerPenaltyHandler?.Invoke(username, pointsLost, newScore, reason);
        }

        public void onGameEnded(MatchEndResultDto result)
        {
            LastMatchResults = result;
            GameEndedStatic?.Invoke(result);
        }

        public void lobbyDestroyed(string reason)
        {
            OnLobbyDestroyedEvent?.Invoke(reason);
        }
       
        public void OnAchievementUnlocked(string achievementKey, string imageName) 
        {
        }

        public void onPlayerLeftMatch(string username)
        {
            OnPlayerLeftEvent?.Invoke(username);
        }

        public static void clearAllHandlers()
        {
            PieceDragStartedHandler = null;
            PiecePlacedHandler = null;
            PieceMovedHandler = null;
            PieceDragReleasedHandler = null;
            PlayerPenaltyHandler = null;
        }
    }
}