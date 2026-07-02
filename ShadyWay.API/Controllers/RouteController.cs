using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShadyWay.API.Dtos;
using ShadyWay.Service;

namespace ShadyWay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpPost]
        public async Task<ActionResult<RouteResponseDto>> CalculateRoute(
            [FromBody] RouteRequestDto request)
        {
            var utcDateTime = request.UtcDateTime ?? DateTime.UtcNow;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _routeService.CalculateRouteAsync(
                request.StartLatitude,  request.StartLongitude,
                request.EndLatitude,    request.EndLongitude,
                request.ShadowPreference,
                utcDateTime,
                userId);

            if (!result.Found)
                return Ok(new RouteResponseDto { Found = false });

            return Ok(new RouteResponseDto
            {
                Found                       = true,
                TotalDistanceMeters         = result.TotalDistanceMeters,
                EstimatedWalkingTimeMinutes = result.EstimatedWalkingTimeMinutes,//זמן הליכה משוער בדקות
                AverageShadowPercentage     = result.AverageShadowPercentage,//אחוז הצל הממוצע על פני כל המסלול
                Path = result.NodePath
                    .Select(n => new CoordinateDto
                    {
                        Latitude  = n.Latitude,
                        Longitude = n.Longitude
                    })
                    .ToList(),
                SegmentShadowPercentages = result.SegmentShadowPercentages//רשימת אחוזי צל לכל קטע בנפרד
            });
        }
    }
}
