using Dapper;
using Dapper_PracticeWithDBRelations.Models;
using System.Data.SqlClient;

namespace Dapper_PracticeWithDBRelations.Repositories
{
    public class VideoGameRepository : IVideoGameRepository
    {
        private readonly string _connectionString;
        public VideoGameRepository(IConfiguration configuration)
        {
            this._connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        public async Task<int> CreateVideoGameAsync(VideoGame videoGame)
        {
            using (var connection = new SqlConnection(this._connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var publisherId = await GetOrCreatePublisherAsync(connection,
                        transaction, videoGame.Developer.Name);

                        int developerId = await GetOrCreateDeveloperAsync(connection,
                              transaction, videoGame.Developer.Name);

                        string sql = @"INSERT INTO VideoGames (Title, PublisherId, DeveloperId, ReleaseDate)
                        Values (@Title,@PublisherId, @DeveloperId, @ReleaseDate)
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                        var id = await connection.QuerySingleAsync<int>(sql, new
                        {
                            videoGame.Title,
                            PublisherId = publisherId,
                            DeveloperId = developerId,
                            videoGame.ReleaseDate
                        }, transaction);

                        videoGame.Id = id;

                        if (videoGame.GameDetail is not null)
                        {
                            videoGame.GameDetail.VideoGameId = id;

                            await CreateGameDetailAsync(connection, videoGame.GameDetail, transaction);
                        }

                        if (videoGame.Reviews is not null)
                        {
                            foreach (var review in videoGame.Reviews)
                            {
                                review.VideoGameId = id;
                                await CreateReviewAsync(connection, review, transaction);
                            }
                        }


                        if (videoGame.Platforms != null)
                        {
                            foreach (var platform in videoGame.Platforms)
                            {
                                await CreateVideoGamePlatformAsync(connection, new VideoGamePlatform
                                {
                                    VideoGameId = id,
                                    PlatformId = platform.Id
                                }, transaction);
                            }
                        }

                        transaction.Commit();

                        return id;

                    }
                    catch (Exception)
                    {
                        transaction.Rollback();

                        throw;
                    }  
                }
            }
        }



        public async Task DeleteVideoGameAsync(int id)
        {
            using(var connection =new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using(var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await DeleteVideoGamePlatformsAsync(connection, id, transaction);
                        await DeleteGameDetailAsync(connection, id, transaction);
                        await DeleteReviewsAsync(connection,id,transaction);


                        string sql = @"DELETE FROM VideoGames WHERE Id=@id;";

                        await connection.ExecuteAsync(sql, new { Id = id},transaction);

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<VideoGame>> GetAllVideoGamesAsync()
        {
            var sql = GetVideoGameSql(false);

            var videoGames= await QueryVideoGamesAsync(sql);

            return videoGames;
        }

        public async Task<VideoGame> GetVideoGameAsync(int id)
        {
            var sql = GetVideoGameSql(true);
            var videoGames = await QueryVideoGamesAsync(sql, new { Id = id });
            return videoGames.FirstOrDefault();
        }

        public async Task UpdateVideoGameAsync(VideoGame videoGame)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using(var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
                        UPDATE VideoGames
                        SET Title = @Title, PublisherId = @PublisherId,
                            DeveloperId = @DeveloperId, ReleaseDate = @ReleaseDate
                        WHERE Id = @Id;";


                        await connection.ExecuteAsync(sql, new
                        {
                            videoGame.Title,
                            videoGame.PublisherId,
                            videoGame.DeveloperId,
                            videoGame.ReleaseDate,
                            videoGame.Id
                        }, transaction);

                        if (videoGame.GameDetail != null)
                        {
                            await UpdateGameDetailAsync(connection, videoGame.GameDetail, transaction);
                        }

                        if (videoGame.Reviews != null)
                        {
                            foreach (var review in videoGame.Reviews)
                            {
                                await UpdateReviewAsync(connection, review, transaction);
                            }
                        }

                        await DeleteVideoGamePlatformsAsync(connection, videoGame.Id, transaction);

                        if (videoGame.Platforms != null)
                        {
                            foreach (var platform in videoGame.Platforms)
                            {
                                await CreateVideoGamePlatformAsync(connection, new VideoGamePlatform
                                {
                                    VideoGameId = videoGame.Id,
                                    PlatformId = platform.Id
                                }, transaction);
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }



        private  string GetVideoGameSql(bool withWhereClause)
        {
            var sql = @"SELECT vg.*, p.*, d.*, gd.*, r.*, pf.*
                    FROM VideoGames vg
                    LEFT JOIN Publishers p ON vg.PublisherId = p.Id
                    LEFT JOIN Developers d ON vg.DeveloperId = d.Id
                    LEFT JOIN GameDetails gd ON vg.Id = gd.VideoGameId
                    LEFT JOIN Reviews r ON vg.Id = r.VideoGameId
                    LEFT JOIN VideoGamesPlatforms vgp ON vg.Id = vgp.VideoGameId
                    LEFT JOIN Platforms pf ON pf.Id = vgp.PlatformId";

            if(withWhereClause)
            {
                sql += " WHERE vg.Id=@Id";
            }

            return sql;
        }


        private async Task<IEnumerable<VideoGame>> QueryVideoGamesAsync(string sql, object? parameters= null)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                var videoGameDictionary= new Dictionary<int,VideoGame>();
                var games = await connection.QueryAsync<VideoGame, Publisher, Developer,
                    GameDetail, Review, Platform, VideoGame>(
                    sql, (videoGame, publisher, developer, gameDetail, review, platform) =>
                    {
                        if (!videoGameDictionary.TryGetValue(videoGame.Id, out var currentGame))
                        {
                            currentGame = videoGame;
                            currentGame.Publisher = publisher;
                            currentGame.Developer = developer;
                            currentGame.GameDetail = gameDetail;
                            currentGame.Reviews = [];
                            currentGame.Platforms = [];
                            videoGameDictionary.Add(currentGame.Id, currentGame);
                        }

                        if (review is not null && !currentGame.Reviews.Any(r => r.Id == review.Id))
                        {
                            currentGame.Reviews.Add(review);
                        }

                        if (platform is not null && !currentGame.Platforms.Any(p => p.Id == platform.Id))
                        {
                            currentGame.Platforms.Add(platform);
                        }

                        return currentGame;
                    }, parameters,
                    splitOn: "Id, Id,VideoGameId,Id,Id");

                return videoGameDictionary.Values;
            }
        }

        private async Task<int>GetOrCreatePublisherAsync(SqlConnection connection,
            SqlTransaction transaction, string publisherName)
        {
            string checkSql = "SELECT * from Publishers where Name=@Name";

            var existingPublisherId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql,
                new { Name = publisherName }, transaction);

            if(existingPublisherId.HasValue)
            {
                return existingPublisherId.Value;
            }


            string insertSql = @"INSERT INTO Publishers (Name) values(@Name)
                            Select CAST(SCOPE_IDENTITY() as int);";

            var newPublisherID = await connection.QuerySingleAsync<int>(insertSql, new { Name = publisherName }, transaction);

            return newPublisherID;
        }


        private async Task<int> GetOrCreateDeveloperAsync(SqlConnection connection,
            SqlTransaction transaction, string developerName)
        {
            string checkSql = "SELECT Id FROM Developers WHERE Name = @Name";
            var existingDeveloperId = await connection
                .QueryFirstOrDefaultAsync<int?>(checkSql, new
                {
                    Name = developerName
                },
                transaction);

            if (existingDeveloperId.HasValue)
            {
                return existingDeveloperId.Value;
            }

            string insertSql = @"INSERT INTO Developers (Name) VALUES (@Name);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            var newDeveloperId = await connection
                .QuerySingleAsync<int>(insertSql, new { Name = developerName },
                transaction);

            return newDeveloperId;
        }

        private async 
             Task CreateGameDetailAsync(SqlConnection connection, GameDetail gameDetail, SqlTransaction transaction)
        {
            string sql = @"INSERT INTO GameDetails (VideoGameId, Description, Rating)
                            VALUES (@VideoGameId, @Description, @Rating);";

            await connection.ExecuteAsync(sql,gameDetail, transaction);
        }

        private async Task CreateReviewAsync(SqlConnection connection, Review review, SqlTransaction transaction)
        {
            string sql = @"INSERT INTO REVIEWS (VideoGameId, ReviewerName, Content, Rating)
                        VALUES (@VideoGameId, @ReviewerName, @Content, @Rating);";

            await connection.ExecuteAsync(sql,review, transaction);
        }

        private async Task CreateVideoGamePlatformAsync(SqlConnection connection, VideoGamePlatform videoGamePlatform, SqlTransaction transaction)
        {
            string sql = @"INSERT INTO VideoGamesPlatforms (VideoGameId, PlatformId)
                            VALUES (@VideoGameId, @PlatformId);";
            await connection.ExecuteAsync(sql, videoGamePlatform, transaction);
        }


        private async Task UpdateGameDetailAsync(SqlConnection connection,GameDetail gameDetail, SqlTransaction transaction)
        {
            string sql = @"UPDATE GameDetails 
                            set Description=@Description, Rating =@Rating
                                where VideoGameId=@VideoGameId";

            await connection.ExecuteAsync(sql, gameDetail, transaction);
        }


        private async Task UpdateReviewAsync(SqlConnection connection,
           Review review, SqlTransaction transaction)
        {
            string sql = @"
                   UPDATE Reviews
                   SET ReviewerName = @ReviewerName, Content = @Content, Rating = @Rating
                   WHERE Id = @Id;";

            await connection.ExecuteAsync(sql, review, transaction);
        }

        private async Task DeleteVideoGamePlatformsAsync(SqlConnection connection,
            int videoGameId, SqlTransaction transaction)
        {
            string sql = @"
                   DELETE FROM VideoGamesPlatforms WHERE VideoGameId = @VideoGameId;";

            await connection.ExecuteAsync(sql, new { VideoGameId = videoGameId }, transaction);
        }

        private async Task DeleteGameDetailAsync(SqlConnection connection,
           int videoGameId, SqlTransaction transaction)
        {
            string sql = @"
                   DELETE FROM GameDetails WHERE VideoGameId = @VideoGameId;";

            await connection.ExecuteAsync(sql, new { VideoGameId = videoGameId }, transaction);
        }

        private async Task DeleteReviewsAsync(SqlConnection connection,
            int videoGameId, SqlTransaction transaction)
        {
            string sql = @"
                   DELETE FROM Reviews WHERE VideoGameId = @VideoGameId;";

            await connection.ExecuteAsync(sql, new { VideoGameId = videoGameId }, transaction);
        }
    }
}
