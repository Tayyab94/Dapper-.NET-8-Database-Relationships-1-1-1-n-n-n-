using Dapper_PracticeWithDBRelations.Models;
using Dapper_PracticeWithDBRelations.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dapper_PracticeWithDBRelations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoGamesController : ControllerBase
    {
        private readonly IVideoGameRepository _videoGameRepository;

        private readonly ILogger<VideoGamesController> _logger;

        public VideoGamesController(ILogger<VideoGamesController> logger, IVideoGameRepository videoGameRepository)
        {
            this._videoGameRepository= videoGameRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<VideoGame>>>GetAllVideoGames()
        {
            var videoGames= await _videoGameRepository.GetAllVideoGamesAsync(); 
            return Ok(videoGames);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<List<VideoGame>>> GetVideoGame(int id)
        {
            var videoGame = await _videoGameRepository.GetVideoGameAsync(id);
            return Ok(videoGame);
        }

        [HttpPost]
        public async Task<ActionResult> CreateVideoGame(VideoGame videoGame)
        {
            if (videoGame == null)
                return BadRequest();

            var createdId = await _videoGameRepository.CreateVideoGameAsync(videoGame);

            return CreatedAtAction(nameof(GetVideoGame), new { id = createdId }, videoGame);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVideoGame(int id, VideoGame videoGame)
        {
            if (videoGame == null || videoGame.Id != id)
            {
                return BadRequest();
            }

            var existingVideoGame = await _videoGameRepository.GetVideoGameAsync(id);
            if (existingVideoGame == null)
            {
                return NotFound();
            }

            await _videoGameRepository.UpdateVideoGameAsync(videoGame);
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVideoGame(int id)
        {
            var existingVideoGame = await _videoGameRepository.GetVideoGameAsync(id);
            if (existingVideoGame == null)
            {
                return NotFound();
            }

            await _videoGameRepository.DeleteVideoGameAsync(id);
            return NoContent();
        }
    }
}
