using DiaryCollector.DataModels;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
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
                            var settings = MongoClientSettings.FromConnectionString(string.Format("mongodb://{0}:{1}@mongo", username, password));
                            settings.WriteConcern = WriteConcern.WMajority;
                            _client = new MongoClient(settings);
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

        public Task<ApiKey> GetApiKeyByKey(string key) {
            var filter = Builders<ApiKey>.Filter.Eq(ak => ak.Key, key);
            return ApiKeys.Find(filter).SingleOrDefaultAsync();
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

        public Task<DailyStats> GetLastDailyStats() {
            var order = Builders<DailyStats>.Sort.Descending(ds => ds.Date);
            return DailyStats.Find(Builders<DailyStats>.Filter.Empty)
                .Sort(order).Limit(1).SingleOrDefaultAsync();
        }

        public Task<IAsyncCursor<DailyStats>> FetchAllDailyStats() {
            var order = Builders<DailyStats>.Sort.Ascending(ds => ds.Date);
            return DailyStats.Find(Builders<DailyStats>.Filter.Empty)
                .Sort(order).ToCursorAsync();
        }

        public Task<IAsyncCursor<DailyStatsAggregation>> GetAggregatedDailyStats() {
            var pipeFilter = BsonDocument.Parse(@"{
                ""$project"": {
                    ""date"": { $dateToString: { ""date"": ""$date"", ""format"": ""%Y-%m-%d"", ""timezone"": ""GMT"" } },
                    ""totalMinutesTracked"": ""$totalMinutesTracked"",
                    ""minutesAtHome"": ""$locationTracking.minutesAtHome"",
                    ""boundingBoxDiagonal"": ""$boundingBoxDiagonal""
                }
            }");
            var pipeGroup = BsonDocument.Parse(@"{
                ""$group"": {
                    ""_id"": ""$date"",
                    ""count"": { ""$sum"": 1 },
                    ""avgMinutesTracked"": { ""$avg"": ""$totalMinutesTracked"" },
                    ""totalMinutesTracked"": { ""$sum"": ""$totalMinutesTracked"" },
                    ""avgMinutesAtHome"": { ""$avg"": ""$minutesAtHome"" },
                    ""totalMinutesAtHome"": { ""$sum"": ""$minutesAtHome"" },
                    ""avgBoundingBoxDiagonal"": { ""$avg"": ""$boundingBoxDiagonal"" }
                }
            }");
            var pipeSort = BsonDocument.Parse(@"{
                ""$sort"": {
                    ""_id"": 1
                }
            }");
            return DailyStats.AggregateAsync<DailyStatsAggregation>(new[] { pipeFilter, pipeGroup, pipeSort });
        }

        #region Call to action

        private IMongoCollection<CallToAction> CallsToAction {
            get {
                return MainDatabase.GetCollection<CallToAction>("CallsToAction");
            }
        }

        public async Task<CallToAction> CreateCallToAction() {
            var call = new CallToAction();
            await CallsToAction.InsertOneAsync(call);
            return call;
        }

        public async Task<CallToAction> GetCallToAction(string id) {
            if(!ObjectId.TryParse(id, out var objId)) {
                return null;
            }
            var filter = Builders<CallToAction>.Filter.Eq(cta => cta.Id, objId);
            return await CallsToAction.Find(filter).FirstOrDefaultAsync();
        }

        public Task UpdateCallToAction(string id, string description, string url, int exposureSeconds) {
            if (!ObjectId.TryParse(id, out var objId)) {
                return Task.CompletedTask;
            }
            
            var filter = Builders<CallToAction>.Filter.Eq(cta => cta.Id, objId);
            var update = Builders<CallToAction>.Update.Combine(
                Builders<CallToAction>.Update.Set(c => c.Description, description),
                Builders<CallToAction>.Update.Set(c => c.Url, url),
                Builders<CallToAction>.Update.Set(c => c.ExposureSeconds, exposureSeconds)
            );
            return CallsToAction.UpdateOneAsync(filter, update);
        }

        #endregion

        #region Call to action filters

        private IMongoCollection<CallToActionFilter> CallToActionFilters {
            get {
                return MainDatabase.GetCollection<CallToActionFilter>("CallToActionFilters");
            }
        }

        public Task<CallToActionFilter> GetCallToActionFilter(string filterId) {
            if (!ObjectId.TryParse(filterId, out var objId)) {
                return Task.FromResult<CallToActionFilter>(null);
            }
            var filter = Builders<CallToActionFilter>.Filter.Eq(cta => cta.Id, objId);
            return CallToActionFilters.Find(filter).SingleOrDefaultAsync();
        }

        public Task AddCallToActionFilter(string callId, CallToActionFilter filter) {
            filter.CallToActionId = new ObjectId(callId);
            return CallToActionFilters.InsertOneAsync(filter);
        }

        public async Task<bool> DeleteCallToActionFilter(string filterId) {
            if (!ObjectId.TryParse(filterId, out var objId)) {
                throw new ArgumentException();
            }
            var filter = Builders<CallToActionFilter>.Filter.Eq(cta => cta.Id, objId);
            var result = await CallToActionFilters.DeleteOneAsync(filter);
            if(!result.IsAcknowledged) {
                throw new InvalidOperationException("Delete not acknowledged");
            }
            return result.DeletedCount == 1;
        }

        /// <summary>
        /// Get list of action filters associated to a given call to action (by ID).
        /// </summary>
        public async Task<List<CallToActionFilter>> GetCallToActionFilters(string callId) {
            if (!ObjectId.TryParse(callId, out var objId)) {
                return null;
            }
            var filter = Builders<CallToActionFilter>.Filter.Eq(cta => cta.CallToActionId, objId);
            return await CallToActionFilters.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Replaces one existing filter.
        /// </summary>
        public Task ReplaceCallToActionFilter(CallToActionFilter f) {
            var filter = Builders<CallToActionFilter>.Filter.Eq(cta => cta.Id, f.Id);
            return CallToActionFilters.ReplaceOneAsync(filter, f);
        }

        #endregion

        private string ConvertHashToRegex(string s) {
            var sb = new StringBuilder(s.Length * 5 - 1);
            sb.Append(s[0]);
            for(int i = 1; i < s.Length; ++i) {
                sb.AppendFormat("({0}|$)", s[i]);
            }

            _logger.LogDebug("Converting hash {0} to regex {1}", s, sb.ToString());

            return sb.ToString();
        }

        public async Task<List<CallToAction>> MatchFilter(DateTime date, string[] geohashes, DateTime lastCheck) {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            _logger.LogDebug("Querying for filters between {0} and {1} in hashes {2}, last check {3}",
                startOfDay, endOfDay, string.Join(", ", geohashes), lastCheck);

            var geofilter = Builders<CallToActionFilter>.Filter.AnyIn(cta => cta.CoveringGeohash, geohashes);

            var filter = Builders<CallToActionFilter>.Filter.And(
                // Check whether filter is newer than last check or ends in the future (i.e., must always be checked)
                Builders<CallToActionFilter>.Filter.Or(
                    Builders<CallToActionFilter>.Filter.Gt(cta => cta.AddedOn, lastCheck),
                    Builders<CallToActionFilter>.Filter.Gt(cta => cta.TimeEnd, lastCheck)
                ),
                // Basic time constraint (filter must have started)
                Builders<CallToActionFilter>.Filter.Lte(cta => cta.TimeBegin, DateTime.UtcNow),
                // Time constraints
                Builders<CallToActionFilter>.Filter.Lt(cta => cta.TimeBegin, endOfDay),
                Builders<CallToActionFilter>.Filter.Gt(cta => cta.TimeEnd, startOfDay),
                // Geo constraints
                Builders<CallToActionFilter>.Filter.Or(geofilter)
            );

            var matching = await CallToActionFilters.Distinct(cta => cta.CallToActionId, filter).ToListAsync();
            _logger.LogDebug("Executed pipeline, got {0} call to action IDs", matching.Count);

            return await GetCallToActions(matching);
        }

        public Task<List<CallToAction>> GetCallToActions(IEnumerable<ObjectId> ids) {
            var filter = Builders<CallToAction>.Filter.In(cta => cta.Id, ids);
            return CallsToAction.Find(filter).ToListAsync();
        }

        public Task<List<CallToAction>> GetAllCallToActions() {
            var filter = Builders<CallToAction>.Filter.Empty;
            return CallsToAction.Find(filter).ToListAsync();
        }

        public Task<List<CallToActionFilter>> GetCallToActionFilters(IEnumerable<ObjectId> callToActionIds) {
            var filter = Builders<CallToActionFilter>.Filter.In(cta => cta.CallToActionId, callToActionIds);
            return CallToActionFilters.Find(filter).ToListAsync();
        }

        private IMongoCollection<User> Users {
            get {
                return MainDatabase.GetCollection<User>("Users");
            }
        }

        public Task<User> GetUserByUsername(string username) {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            return Users.Find(filter).SingleOrDefaultAsync();
        }

    }

}
