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
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly string bootstrapServers = "localhost:9092";
        private readonly string topic = "user";

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [ActionName("all")]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            try
            {
                var user = await _userService.GetAllAsync();
                return Ok(user);
            }

            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpGet("{username}")]
        [ActionName("search")]
        public async Task<ActionResult<User>> GetByUsername(string username)
        {
            try
            {
                var user = await _userService.GetByUsernameAsync(null, username);

                if (user == null)
                {
                    return NotFound();
                }

                return user;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [ActionName("register")]
        public async Task<IActionResult> CreateUser(User user)
        {
            try
            {
                var user1 = await _userService.GetByUsernameAsync(null, user.Login_Id);
                var user2 = await _userService.GetByUsernameAsync(null, user.Email);

                if (user1 == null && user2 == null)
                {
                    await _userService.CreateUserAsync(user);
                    return Ok(user);
                }
                else
                {
                    return user1 != null ? BadRequest("Login Id already exists") : BadRequest("Email already exists");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [AllowAnonymous]
        [HttpPut]
        [ActionName("{Login_Id}/forgot")]
        public async Task<IActionResult> ForgotPassword(User user)
        {
            try
            {
                var queriedUser = await _userService.GetByUsernameAsync(user, "");
                if (queriedUser == null)
                {
                    return NotFound();
                }

                if (user.Password == queriedUser.Password)
                {
                    //var error = "New password cannot be same as the old password "; //<-- anonymous object
                    return BadRequest("New password cannot be same as the old password");
                }
                await _userService.UpdatePasswordAsync(queriedUser, user);
                return NoContent();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [ActionName("login")]
        public IActionResult Login([FromBody] User user)
        {
            try
            {
                var userDetails = _userService.GetByUsernameAsync(null, user.Email);

                var token = _userService.Authenticate(user.Email, user.Password);

                if (token == "")
                {
                    return NotFound();
                }
                else if (token == null)
                {
                    return Unauthorized();
                }
                return Ok(new { token, userDetails });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ActionName("logout")]
        public IActionResult Logout([FromBody] User user)
        {
            try
            {
                _userService.Logout(user.Email);
                return Ok();
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
                using (var producer = new ProducerBuilder
                <Null, string>(config).Build())
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
