using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using TweetMe.Models;
using TweetMe.Services;

namespace TweetMe.Controllers
{
    [Authorize]
    [Route("api/v1.0/[controller]/[action]")]
    [ApiController]
    public class TweetController : Controller
    {
        private readonly TweetService _tweetService;
        private readonly string bootstrapServers = "localhost:9092";
        private readonly string topic = "tweet";
        private readonly ILogger<TweetController> _logger;

        public TweetController(TweetService tweetService, ILogger<TweetController> logger)
        {
            _tweetService = tweetService;
            _logger = logger;
        }

        [HttpGet]
        [ActionName("all")]
        public async Task<ActionResult<IEnumerable<Tweet>>> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAll tweet API called");
                var user = await _tweetService.GetAllTweet();
                _logger.LogInformation("Successfully returned all tweets");
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAll tweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpGet("{Login_Id}")]
        [ActionName("user")]
        public async Task<ActionResult<IEnumerable<Tweet>>> GetByUsername(string Login_Id)
        {
            try
            {
                _logger.LogInformation("GetByUsername API called");
                var tweet = await _tweetService.GetByUsernameAsync(Login_Id);

                if (tweet == null)
                {
                    _logger.LogInformation("GetByUsername API: Tweet not found");
                    return NotFound();
                }

                _logger.LogInformation("Successfully returned tweets for given user {0}", Login_Id);
                return tweet;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetByUsername API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ActionName("{Login_Id}/add")]
        public async Task<IActionResult> CreateTweet(string Login_Id, Tweet tweet)
        {
            try
            {
                _logger.LogInformation("CreateTweet API called");
                await _tweetService.CreateTweetAsync(tweet);

                _logger.LogInformation("Successfully created new tweet");

                return Ok(tweet);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateTweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ActionName("{Login_Id}/update")]
        public async Task<IActionResult> UpdateTweet(Tweet updatedTweet)
        {
            try
            {
                _logger.LogInformation("UpdateTweet API called");
                var queriedTweet = await _tweetService.GetByIdAsync(updatedTweet.Id);
                if (queriedTweet == null)
                {
                    _logger.LogInformation("UpdateTweet API: Tweet not found");
                    return NotFound();
                }
                await _tweetService.UpdateTweetAsync(updatedTweet.Id, updatedTweet);

                _logger.LogInformation("Successfully updated the tweet");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateTweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}")]
        [ActionName("{Login_Id}/delete")]
        public async Task<IActionResult> DeleteTweet(string id)
        {
            try
            {
                _logger.LogInformation("DeleteTweet API called");
                var tweet = await _tweetService.GetByIdAsync(id);
                if (tweet == null)
                {
                    _logger.LogInformation("DeleteTweet API: Tweet not found");
                    return NotFound();
                }
                await _tweetService.DeleteTweetAsync(id);

                _logger.LogInformation("Successfully deleted the tweet");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteTweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpPut("{id}")]
        [ActionName("{Login_Id}/like")]
        public async Task<IActionResult> LikeTweet(string id, Tweet updatedTweet)
        {
            try
            {
                _logger.LogInformation("LikeTweet API called");
                var queriedTweet = await _tweetService.GetByIdAsync(id);
                if (queriedTweet == null)
                {
                    _logger.LogInformation("LikeTweet API: Tweet not found");
                    return NotFound();
                }
                await _tweetService.LikeTweetAsync(id, updatedTweet, queriedTweet);

                _logger.LogInformation("Successfully liked the tweet");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("LikeTweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpPut("{id}")]
        [ActionName("{Login_Id}/reply")]
        public async Task<IActionResult> ReplyTweet(string id, Tweet tweet)
        {
            try
            {
                _logger.LogInformation("ReplyTweet API called");
                var oldTweet = await _tweetService.GetByIdAsync(id);
                if (oldTweet == null)
                {
                    _logger.LogInformation("ReplyTweet API: Tweet not found");
                    return NotFound();
                }

                await _tweetService.ReplyTweetAsync(id, oldTweet, tweet);

                _logger.LogInformation("Successfully added reply to the tweet");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("ReplyTweet API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        private async Task<bool> SendOrderRequest(string topic, string message)
        {
            ProducerConfig config = new()
            {
                BootstrapServers = bootstrapServers,
                ClientId = Dns.GetHostName()
            };

            try
            {
                using (var producer = new ProducerBuilder<Null, string>(config).Build())
                {
                    var result = await producer.ProduceAsync
                    (topic, new Message<Null, string>
                    {
                        Value = message
                    });

                    Debug.WriteLine($"Delivery Timestamp: { result.Timestamp.UtcDateTime }");
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
            }

            return await Task.FromResult(false);
        }
    }
}
