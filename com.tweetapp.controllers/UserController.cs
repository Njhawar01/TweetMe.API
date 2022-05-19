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
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly string bootstrapServers = "localhost:9092";
        private readonly string topic = "user";
        private readonly ILogger<UserController> _logger;

        public UserController(UserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [ActionName("all")]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAll user API called");
                var user = await _userService.GetAllAsync();

                _logger.LogInformation("Successfully returned all users");
                return Ok(user);
            }

            catch(Exception ex)
            {
                _logger.LogError("GetAll user API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpGet("{username}")]
        [ActionName("search")]
        public async Task<ActionResult<User>> GetByUsername(string username)
        {
            try
            {
                _logger.LogInformation("GetByUsername API called");
                var user = await _userService.GetByUsernameAsync(null, username);

                if (user == null)
                {
                    _logger.LogInformation("GetByUsername API: user not found");
                    return NotFound();
                }

                _logger.LogInformation("Successfully returned details for the user {0}", username);
                return user;
            }
            catch(Exception ex)
            {
                _logger.LogError("GetByUsername API exception: {0}", ex);
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
                _logger.LogInformation("CreateUser API called");
                var user1 = await _userService.GetByUsernameAsync(null, user.Login_Id);
                var user2 = await _userService.GetByUsernameAsync(null, user.Email);

                if (user1 == null && user2 == null)
                {
                    await _userService.CreateUserAsync(user);
                    _logger.LogInformation("Successfully registered new user");
                    return Ok(user);
                }
                else
                {
                    string error = user1 != null ? "Login Id already exists" : "Email already exists";
                    _logger.LogError("Error occured while registering the user: {0}", error);
                    return BadRequest(error);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("CreateUser API exception: {0}", ex);
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
                _logger.LogInformation("ForgotPassword API called");
                var queriedUser = await _userService.GetByUsernameAsync(user, "");
                if (queriedUser == null)
                {
                    _logger.LogInformation("ForgotPassword API: user not found");
                    return NotFound();
                }

                if (user.Password == queriedUser.Password)
                {
                    string error = "New password cannot be same as the old password ";
                    _logger.LogError("Error occured while resetting the password: {0}", error);
                    return BadRequest(error);
                }
                await _userService.UpdatePasswordAsync(queriedUser, user);

                _logger.LogInformation("Password reset successful");

                return NoContent();
            }
            catch(Exception ex)
            {
                _logger.LogError("ForgotPassword API exception: {0}", ex);
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
                _logger.LogInformation("Login API called");
                var userDetails = _userService.GetByUsernameAsync(null, user.Email);

                var token = _userService.Authenticate(user.Email, user.Password);

                if (token == "")
                {
                    _logger.LogInformation("Login API: user not found");
                    return NotFound();
                }
                else if (token == null)
                {
                    _logger.LogInformation("Login API: unauthorized user");
                    return Unauthorized();
                }

                _logger.LogInformation("Login API: user not found successful");
                return Ok(new { token, userDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError("Login API exception: {0}", ex);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ActionName("logout")]
        public IActionResult Logout([FromBody] User user)
        {
            try
            {
                _logger.LogInformation("Logout API called");
                _userService.Logout(user.Email);

                _logger.LogInformation("User successfully logged out");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Logout API exception: {0}", ex);
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
