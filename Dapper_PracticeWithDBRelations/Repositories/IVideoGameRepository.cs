using Dapper_PracticeWithDBRelations.Models;

namespace Dapper_PracticeWithDBRelations.Repositories
{
    public interface IVideoGameRepository
    {
        Task<int> CreateVideoGameAsync(VideoGame videoGame);
        Task<VideoGame> GetVideoGameAsync(int id);
        Task<IEnumerable<VideoGame>> GetAllVideoGamesAsync();
        Task UpdateVideoGameAsync(VideoGame videoGame);
        Task DeleteVideoGameAsync(int id);
    }
}
