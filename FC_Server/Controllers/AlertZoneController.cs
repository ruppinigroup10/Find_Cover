using Microsoft.AspNetCore.Mvc;
using FC_Server.Models;
using System;
using System.Linq;

namespace FC_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertZoneController : ControllerBase
    {
        private readonly AlertManager _alertManager;

        public AlertZoneController()
        {
            _alertManager = new AlertManager();
        }

        // בדיקת אזור התרעה והאם יש התרעה פעילה
        [HttpGet("check")]
        public IActionResult CheckLocation(double lat, double lng)
        {
            try
            {
                var result = _alertManager.CheckActiveAlertForLocation(lat, lng);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // קבלת כל האזורים (למפה)
        [HttpGet("all")]
        public IActionResult GetAllZones()
        {
            try
            {
                var zones = _alertManager.GetAllZones();
                var result = zones.Select(z => new
                {
                    z.ZoneId,
                    z.ZoneName,
                    z.ResponseTime,
                    Coordinates = z.Coordinates
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // בדיקה אם משתמש נמצא באזור ספציפי
        [HttpPost("contains-point")]
        public IActionResult CheckIfPointInZone([FromBody] LocationCheckRequest request)
        {
            try
            {
                if (request == null || request.Latitude == 0 || request.Longitude == 0)
                {
                    return BadRequest(new { error = "Invalid location data" });
                }

                var result = _alertManager.CheckActiveAlertForLocation(
                    request.Latitude,
                    request.Longitude
                );

                return Ok(new
                {
                    isInZone = result.IsInAlertZone,
                    zoneName = result.ZoneName,
                    responseTime = result.ResponseTime,
                    hasActiveAlert = result.HasActiveAlert,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class LocationCheckRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}