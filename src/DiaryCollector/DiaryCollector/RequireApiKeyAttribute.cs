using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector {
    
    public class RequireApiKeyAttribute : ActionFilterAttribute {

        private readonly MongoConnector Mongo;
        private readonly ILogger<RequireApiKeyAttribute> Logger;

        public const string ApiKeyHeader = "Diary-Key";

        public RequireApiKeyAttribute(
            MongoConnector mongo,
            ILogger<RequireApiKeyAttribute> logger
        ) {
            Mongo = mongo;
            Logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            Logger.LogTrace("Checking API key access");

            var request = context.HttpContext.Request;
            if(!request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyHeader)) {
                Logger.LogError("API key not set");
                context.Result = new UnauthorizedResult();
                return;
            }
            Logger.LogTrace("API key '{0}' supplied", apiKeyHeader);

            var apiKey = await Mongo.GetApiKeyByKey(apiKeyHeader.ToString());
            if(apiKey == null) {
                Logger.LogError("API key not valid");
                context.Result = new UnauthorizedResult();
                return;
            }

            Logger.LogDebug("Access granted for API key {0}", apiKey.Key);
            await next();
        }

    }

}
