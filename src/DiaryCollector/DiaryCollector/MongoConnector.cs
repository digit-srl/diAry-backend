using DiaryCollector.DataModels;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector {

    public class MongoConnector {

        private readonly ILogger<MongoConnector> _logger;

        public MongoConnector(
            ILogger<MongoConnector> logger) {
            _logger = logger;
        }

        private readonly object _lockRoot = new object();
        private MongoClient _client = null;

        private MongoClient Client {
            get {
                if (_client == null) {
                    lock (_lockRoot) {
                        if (_client == null) {
                            var username = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_USERNAME");
                            var password = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD");

                            _logger.LogInformation("Creating new Mongo client");
                            _client = new MongoClient(string.Format("mongodb://{0}:{1}@mongo", username, password));
                        }
                    }
                }

                return _client;
            }
        }

        private IMongoDatabase MainDatabase {
            get {
                return Client.GetDatabase("DiaryCollection");
            }
        }

        private IMongoCollection<DailyStats> DailyStats {
            get {
                return MainDatabase.GetCollection<DailyStats>("DailyStats");
            }
        }

        public Task AddDailyStats(DailyStats stats) {
            return DailyStats.InsertOneAsync(stats);
        }

    }

}
