using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetMe.Models;

namespace TweetMe.Services
{
    public class TweetService
    {
        private readonly IMongoCollection<Tweet> _tweets;
        public TweetService(ITweetMeDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _tweets = database.GetCollection<Tweet>(settings.TweetsCollectionName);
        }

        public async Task<List<Tweet>> GetAllTweet()
        {
            return await _tweets.Find(tweet => true).SortByDescending(d => d.Date).ToListAsync();
        }

        public async Task<List<Tweet>> GetByUsernameAsync(string username)
        {
            return await _tweets.Find<Tweet>(Tweet => Tweet.Login_Id == username).SortByDescending(d => d.Date).ToListAsync();
        }

        public async Task<Tweet> GetByIdAsync(string id)
        {
            return await _tweets.Find<Tweet>(Tweet => Tweet.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Tweet> CreateTweetAsync(Tweet tweet)
        {
            tweet.Date = DateTime.Now;
            await _tweets.InsertOneAsync(tweet);
            return tweet;
        }

        public async Task UpdateTweetAsync(string id, Tweet queriedTweet)
        {
            await _tweets.ReplaceOneAsync(s => s.Id == id, queriedTweet);
        }

        public async Task LikeTweetAsync(string id, Tweet updatedTweet, Tweet queriedTweet)
        {
            queriedTweet.Like += 1;
            queriedTweet.Liked_By = updatedTweet.Liked_By;
            await _tweets.ReplaceOneAsync(s => s.Id == id, queriedTweet);
        }
        public async Task DeleteTweetAsync(string id)
        {
            await _tweets.DeleteOneAsync(s => s.Id == id);
        }

        public async Task ReplyTweetAsync(string id, Tweet oldTweet, Tweet tweet)
        {
            foreach (var r in tweet.Reply)
            {
                oldTweet.Reply = tweet.Reply;
            }
            await _tweets.ReplaceOneAsync(s => s.Id == id, oldTweet);
        }
    }
}
