using DiaryCollector.DataModels;
using Geohash;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private IMongoCollection<ApiKey> ApiKeys {
            get {
                return MainDatabase.GetCollection<ApiKey>("ApiKeys");
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

        public Task<DailyStats> GetDailyStats(Guid deviceId, DateTime date) {
            var filter = Builders<DailyStats>.Filter.And(
                Builders<DailyStats>.Filter.Eq(s => s.InstallationId, deviceId),
                Builders<DailyStats>.Filter.Eq(s => s.Date, date.Date)
            );
            return DailyStats.Find(filter).SingleOrDefaultAsync();
        }

        public Task<ApiKey> GetApiKeyByKey(string key) {
            var filter = Builders<ApiKey>.Filter.Eq(ak => ak.Key, key);
            return ApiKeys.Find(filter).SingleOrDefaultAsync();
        }

        private IMongoCollection<CallToAction> CallsToAction {
            get {
                return MainDatabase.GetCollection<CallToAction>("CallsToAction");
            }
        }

        private IMongoCollection<CallToActionFilter> CallToActionFilters {
            get {
                return MainDatabase.GetCollection<CallToActionFilter>("CallToActionFilters");
            }
        }

        private string ConvertHashToRegex(string s) {
            var sb = new StringBuilder(s.Length * 5 - 1);
            sb.Append(s[0]);
            for(int i = 1; i < s.Length; ++i) {
                sb.AppendFormat("({0}|$)", s[i]);
            }

            _logger.LogDebug("Converting hash {0} to regex {1}", s, sb.ToString());

            return sb.ToString();
        }

        public async Task<List<CallToAction>> MatchFilter(DateTime date, string[] geohashes) {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(0.999);

            _logger.LogDebug("Querying for filters between {0} and {1} in hashes {2}",
                startOfDay, endOfDay, string.Join(", ", geohashes));

            var geofilters = from hash in geohashes
                             let regex = ConvertHashToRegex(hash)
                             select Builders<CallToActionFilter>.Filter.Regex(cta => cta.CoveringGeohash,
                                 new BsonRegularExpression(regex)
                             );

            var filter = Builders<CallToActionFilter>.Filter.And(
                Builders<CallToActionFilter>.Filter.Lt(cta => cta.TimeBegin, startOfDay),
                Builders<CallToActionFilter>.Filter.Gt(cta => cta.TimeEnd, endOfDay),
                Builders<CallToActionFilter>.Filter.Or(geofilters)
            );

            var callIdProjection = Builders<CallToActionFilter>.Projection.Expression(
                cta => cta.CallToActionId
            );

            var pipeline = PipelineDefinitionBuilder.For<CallToActionFilter>()
                .Match(filter)
                .Group(callIdProjection);
            
            var matching = await CallToActionFilters.Aggregate(pipeline).ToListAsync();
            _logger.LogDebug("Executed pipeline, got {0} call to action IDs", matching.Count);

            return await GetCallToActions(matching);
        }

        public Task<List<CallToAction>> GetCallToActions(IEnumerable<ObjectId> ids) {
            var filter = Builders<CallToAction>.Filter.In(cta => cta.Id, ids);
            return CallsToAction.Find(filter).ToListAsync();
        }

    }

}
