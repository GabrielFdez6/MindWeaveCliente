using MindWeaveClient.MatchmakingService;
using System;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Abstractions
{
    public interface IMatchmakingService
    {
        event Action<LobbyStateDto> OnLobbyStateUpdated;
        event Action<string> OnLobbyCreationFailed;
        event Action<string> OnKicked;
        event Action<string> OnLobbyActionFailed;
        event Action<string> OnLobbyDestroyed;
        event Action<MatchFoundDto> OnMatchFound;
        event Action<PuzzleDefinitionDto, int> OnGameStarted;
        Task<LobbyCreationResultDto> createLobbyAsync(string hostUsername, LobbySettingsDto settings);
        Task<GuestJoinServiceResultDto> joinLobbyAsGuestAsync(GuestJoinRequestDto request);
        Task<JoinLobbyResultDto> joinLobbyWithConfirmationAsync(string username, string lobbyCode);
        Task leaveLobbyAsync(string username, string lobbyId);
        Task startGameAsync(string hostUsername, string lobbyId);
        Task kickPlayerAsync(string hostUsername, string playerToKick, string lobbyId);
        Task inviteToLobbyAsync(string inviter, string invited, string lobbyId);
        Task inviteGuestByEmailAsync(GuestInvitationDto invitationData);
        void disconnect(bool forceAbort = false);
        Task requestPieceDragAsync(string lobbyCode, int pieceId);
        Task requestPieceMoveAsync(string lobbyCode, PieceMovementDto movement);
        Task requestPieceDropAsync(string lobbyCode, PieceMovementDto movement);
        Task leaveGameAsync(string username, string lobbyCode);
    }
}