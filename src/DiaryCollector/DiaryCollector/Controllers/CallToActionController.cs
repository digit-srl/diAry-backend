using DiaryCollector.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {

    [Route("calls")]
    public class CallToActionController : Controller {

        private readonly MongoConnector Mongo;
        private readonly WomService Wom;
        private readonly LinkGenerator Link;
        private readonly ILogger<CallToActionController> Logger;
        private readonly Geohash.Geohasher Geohasher = new Geohash.Geohasher();

        public CallToActionController(
            MongoConnector mongo,
            WomService wom,
            LinkGenerator linkGenerator,
            ILogger<CallToActionController> logger
        ) {
            Mongo = mongo;
            Wom = wom;
            Link = linkGenerator;
            Logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Show(
            [FromRoute] string id
        ) {
            var cta = await Mongo.GetCallToAction(id);
            if(cta == null) {
                Logger.LogInformation("Call to action {0} not found", id);
                return NotFound();
            }

            var filter = (await Mongo.GetCallToActionFilters(id)).FirstOrDefault();
            if(filter == null) {
                Logger.LogError("Call to action {0} has no filter", filter);
                return NotFound();
            }

            var geohashBounds = Geohasher.GetBoundingBox(filter.CoveringGeohash);

            return View("Show", new CallToActionViewModel {
                Id = cta.Id.ToString(),
                From = filter.TimeBegin,
                To = filter.TimeEnd,
                Description = cta.Description,
                BoundingBox = geohashBounds,
                PolygonCoordinates = filter.Geometry.ToRingArray()
            });
        }

    }

}
