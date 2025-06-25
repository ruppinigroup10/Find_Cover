using Microsoft.AspNetCore.Mvc;
using FC_Server.Models;


namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly DBservicesLocation _locationService;

        public LocationController(DBservicesLocation locationService)
        {
            _locationService = locationService;
        }

        // POST api/Location/AddUserLocation
        [HttpPost("AddUserLocation")]
        public IActionResult AddUserLocation([FromBody] UserLocation location)
        {
            try
            {
                _locationService.InsertUserLocation(location);
                return Ok(new { message = "Location added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to add location", error = ex.Message });
            }
        }
    }
}