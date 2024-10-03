namespace Dapper_PracticeWithDBRelations.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int VideoGameId { get; set; }
        public required string ReviewerName { get; set; }
        public required string Content { get; set; }
        public int Rating { get; set; }

        // Navigation property
        public VideoGame? VideoGame { get; set; }
    }
}
