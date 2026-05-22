using MindWeaveClient.PuzzleManagerService;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Abstractions
{
    public interface IPuzzleService
    {
        Task<PuzzleInfoDto[]> getAvailablePuzzlesAsync();

        Task<UploadResultDto> uploadPuzzleImageAsync(string username, byte[] imageBytes, string fileName);
    }
}
