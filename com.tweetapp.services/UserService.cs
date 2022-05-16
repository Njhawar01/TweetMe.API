using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TweetMe.Models;

namespace TweetMe.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly string key;
        public UserService(ITweetMeDatabaseSettings settings, IConfiguration configuration)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>(settings.UsersCollectionName);
            this.key = configuration.GetSection("JwtToken").ToString();
        }

        public string Authenticate(string email, string password)
        {
            var user = this._users.Find(x => (x.Email == email || x.Login_Id == email)).FirstOrDefault();
            dynamic userDetails;
            if (user == null)
            {
                return "";
            }
            else
            {
                userDetails = this._users.Find(x => (x.Email == email || x.Login_Id == email) && x.Password == password).FirstOrDefault();
                if(userDetails == null)
                {
                    return null;
                }
            }
            //var user = this._users.Find(x => (x.Email == email || x.Login_Id == email) && x.Password == password).FirstOrDefault();
            

            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenKey = Encoding.ASCII.GetBytes(key);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, email),
                }),

                Expires = DateTime.UtcNow.AddHours(1),

                SigningCredentials = new SigningCredentials
                (
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            if(token != null)
            {
                user.Login_Status = true;
                _users.ReplaceOneAsync(s => s.Login_Id == user.Login_Id, user);
            }

            return tokenHandler.WriteToken(token);
        }

        public void Logout(string email)
        {
            var user = this._users.Find(x => x.Email == email || x.Login_Id == email).FirstOrDefault();
            if (user != null)
            {
                user.Login_Status = false;
                _users.ReplaceOneAsync(s => s.Login_Id == user.Login_Id, user);
            }
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _users.Find(user => true).ToListAsync();
        }

        public async Task<User> GetByUsernameAsync(User credentials, string username)
        {
            if (credentials != null)
            {
                return await _users.Find<User>(user => user.Login_Id == credentials.Login_Id || user.Email == credentials.Login_Id).FirstOrDefaultAsync();
            }
            else
            {
                return await _users.Find<User>(user => user.Login_Id == username || user.Email == username).FirstOrDefaultAsync();
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
            return user;
        }

        public async Task UpdatePasswordAsync(User queriedUser, User user)
        {
            queriedUser.Password = user.Password;
            await _users.ReplaceOneAsync(s => s.Login_Id == user.Login_Id, queriedUser);
        }
    }
}
