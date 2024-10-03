namespace Dapper_PracticeWithDBRelations.Models
{
    public class Platform
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        // Navigation property
        public List<VideoGame> VideoGames { get; set; } = [];
    }
}
