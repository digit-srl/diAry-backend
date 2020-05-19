using DiaryCollector.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {

    [Route("dashboard")]
    [Authorize(Startup.AdminUserLoginPolicy)]
    public class DashboardController : BaseController {

        private readonly MongoConnector Mongo;

        public DashboardController(
            MongoConnector mongoConnector,
            LinkGenerator linker,
            ILogger<DashboardController> logger
        ) : base(linker, logger) {
            Mongo = mongoConnector;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var calls = await Mongo.GetAllCallToActions();
            
            return View("Index", new DashboardMainViewModel {
                Calls = calls
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ShowCall(
            [FromRoute] string id
        ) {
            var call = await Mongo.GetCallToAction(id);
            if(call == null) {
                return NotFound();
            }

            var filters = await Mongo.GetCallToActionFilters(id);

            return View("CallToAction", new DashboardCallToActionViewModel {
                Call = call,
                Filters = filters
            });
        }

        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeleteCall(
            [FromRoute] string id
        ) {
            var call = await Mongo.GetCallToAction(id);
            if (call == null) {
                return NotFound();
            }

            return BadRequest();
        }

        [HttpPost("{id}/data")]
        public async Task<IActionResult> UpdateCall(
            [FromRoute] string id,
            [FromForm] string description,
            [FromForm] string url
        ) {
            await Mongo.UpdateCallToAction(id, description, url);

            return RedirectToAction(nameof(ShowCall), "Dashboard", new { id = id });
        }

        [HttpPost("{id}/{filterId}")]
        public async Task<IActionResult> UpdateFilter(
            [FromRoute] string id,
            [FromRoute] string filterId,
            [FromForm] DateTime? addedOn,
            [FromForm] DateTime from,
            [FromForm] DateTime to,
            [FromForm] string geojson
        ) {
            var filter = await Mongo.GetCallToActionFilter(filterId);
            if (filter == null || !filter.CallToActionId.ToString().Equals(id)) {
                return NotFound();
            }

            filter.AddedOn = addedOn;
            filter.AddedOn = addedOn ?? DateTime.UtcNow;
            filter.TimeBegin = from;
            filter.TimeEnd = to;
            filter.Geometry = geojson.PolygonFromGeoJson();

            await Mongo.ReplaceCallToActionFilter(filter);

            return RedirectToAction(nameof(ShowCall), "Dashboard", new { id = id });
        }

        [HttpPost("{id}/{filterId}/delete")]
        public async Task<IActionResult> DeleteFilter(
            [FromRoute] string id,
            [FromRoute] string filterId
        ) {
            var filter = await Mongo.GetCallToActionFilter(filterId);
            if (filter == null || !filter.CallToActionId.ToString().Equals(id)) {
                return NotFound();
            }

            var result = await Mongo.DeleteCallToActionFilter(filterId);
            Logger.LogInformation("Deleted filter #{0} with result {1}", filterId, result);

            return RedirectToAction(nameof(ShowCall), "Dashboard", new { id = id });
        }

    }

}
