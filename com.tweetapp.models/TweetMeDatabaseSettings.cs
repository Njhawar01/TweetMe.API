using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TweetMe.Models
{
    public class TweetMeDatabaseSettings : ITweetMeDatabaseSettings
    {
        public string UsersCollectionName { get; set; }
        public string TweetsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface ITweetMeDatabaseSettings
    {
        public string UsersCollectionName { get; set; }
        public string TweetsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
