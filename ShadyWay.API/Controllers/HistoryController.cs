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
    public class HistoryController : ControllerBase
    {
        private readonly IRouteHistoryService _historyService;

        public HistoryController(IRouteHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet]
        public async Task<ActionResult<List<RouteHistoryDto>>> GetHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);//שליפת מזהה המשתמש מהטוקן

            var items = await _historyService.GetHistoryAsync(userId);

            var result = items.Select(i => new RouteHistoryDto
            {
                RequestId           = i.RequestId,
                SourceAddress       = i.SourceAddress,
                DestAddress         = i.DestAddress,
                RequestTime         = i.RequestTime,
                TotalDistanceMeters = i.TotalDistanceMeters,
                ShadowPercentage    = i.ShadowPercentage
            }).ToList();

            return Ok(result);
        }
    }
}
