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
    public class CallToActionController : BaseController {

        private readonly MongoConnector Mongo;

        public CallToActionController(
            MongoConnector mongo,
            LinkGenerator linkGenerator,
            ILogger<CallToActionController> logger
        ) : base(linkGenerator, logger)
        {
            Mongo = mongo;
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

            // HACK: shows huge Geohash
            var geohashBounds = Geohasher.GetBoundingBox(filter.CoveringGeohash[0].Substring(0, 1));

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
