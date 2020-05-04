using Geohash;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {
    
    public class BaseController : Controller {

        protected readonly string SelfHostDomain;
        protected readonly ILogger<BaseController> Logger;
        protected readonly LinkGenerator Linker;

        protected readonly Geohasher Geohasher = new Geohasher();

        public BaseController(
            LinkGenerator linker,
            ILogger<BaseController> logger
        ) {
            Logger = logger;
            Linker = linker;
            SelfHostDomain = Environment.GetEnvironmentVariable("SELF_HOST");
        }

        protected string GenerateActionLink(string action, string controller, object routeValues = null) {
            return Linker.GetUriByAction(
                action,
                controller,
                routeValues,
                "https",
                new HostString(SelfHostDomain)
            );
        }

    }

}
