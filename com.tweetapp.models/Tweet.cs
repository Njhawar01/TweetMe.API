using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetMe.com.tweetapp.models;

namespace TweetMe.Models
{
    public class Tweet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Login_Id { get; set; }

        public string Post { get; set; }

        public DateTime Date { get; set; }

        public int Like { get; set; }

        public string[] Liked_By { get; set; }

        public List<TweetReply> Reply { get; set; }
    }
}
