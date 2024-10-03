namespace Dapper_PracticeWithDBRelations.Models
{
    public class GameDetail
    {
        public int VideoGameId { get; set; }
        public required string Description { get; set; }
        public required string Rating { get; set; }

        // Navigation property
        public VideoGame? VideoGame { get; set; }
    }
}
