namespace Dapper_PracticeWithDBRelations.Models
{
    public class VideoGame
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int PublisherId { get; set; }
        public int DeveloperId { get; set; }
        public DateTime ReleaseDate { get; set; }

        public Publisher? Publisher { get; set; }
        public Developer? Developer { get; set; }

        // Navigation properties
        public GameDetail? GameDetail { get; set; }
        public List<Review> Reviews { get; set; } = [];
        public List<Platform> Platforms { get; set; } = [];
    }
}
