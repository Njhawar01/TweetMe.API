using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
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

        public TweetController(TweetService tweetService)
        {
            _tweetService = tweetService;
        }

        [HttpGet]
        [ActionName("all")]
        public async Task<ActionResult<IEnumerable<Tweet>>> GetAll()
        {
            try
            {
                var user = await _tweetService.GetAllTweet();
                return Ok(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpGet("{Login_Id}")]
        [ActionName("user")]
        public async Task<ActionResult<IEnumerable<Tweet>>> GetByUsername(string Login_Id)
        {
            try
            {
                var tweet = await _tweetService.GetByUsernameAsync(Login_Id);

                if (tweet == null)
                {
                    return NotFound();
                }

                return tweet;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ActionName("{Login_Id}/add")]
        public async Task<IActionResult> CreateTweet(string Login_Id, Tweet tweet)
        {
            try
            {
                await _tweetService.CreateTweetAsync(tweet);
                return Ok(tweet);

                //string message = JsonSerializer.Serialize(tweet);
                //return Ok(await SendOrderRequest(topic, message));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ActionName("{Login_Id}/update")]
        public async Task<IActionResult> UpdateTweet(Tweet updatedTweet)
        {
            try
            {
                var queriedTweet = await _tweetService.GetByIdAsync(updatedTweet.Id);
                if (queriedTweet == null)
                {
                    return NotFound();
                }
                await _tweetService.UpdateTweetAsync(updatedTweet.Id, updatedTweet);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}")]
        [ActionName("{Login_Id}/delete")]
        public async Task<IActionResult> DeleteTweet(string id)
        {
            try
            {
                var tweet = await _tweetService.GetByIdAsync(id);
                if (tweet == null)
                {
                    return NotFound();
                }
                await _tweetService.DeleteTweetAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpPut("{id}")]
        [ActionName("{Login_Id}/like")]
        public async Task<IActionResult> LikeTweet(string id, Tweet updatedTweet)
        {
            try
            {
                var queriedTweet = await _tweetService.GetByIdAsync(id);
                if (queriedTweet == null)
                {
                    return NotFound();
                }
                await _tweetService.LikeTweetAsync(id, updatedTweet, queriedTweet);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpPut("{id}")]
        [ActionName("{Login_Id}/reply")]
        public async Task<IActionResult> ReplyTweet(string id, Tweet tweet)
        {
            try
            {
                var oldTweet = await _tweetService.GetByIdAsync(id);
                if (oldTweet == null)
                {
                    return NotFound();
                }

                await _tweetService.ReplyTweetAsync(id, oldTweet, tweet);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        private async Task<bool> SendOrderRequest(string topic, string message)
        {
            ProducerConfig config = new ProducerConfig
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
