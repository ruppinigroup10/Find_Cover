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
        private readonly ILogger<AlertZoneController> _logger;

        public AlertZoneController(ILogger<AlertZoneController> logger)
        {
            _logger = logger;
            _alertManager = new AlertManager();
        }

        // בדיקת התרעה למשתמש לפי המיקום האחרון שלו
        [HttpGet("check-user/{userId}")]
        public IActionResult CheckUserAlert(int userId)
        {
            try
            {
                var result = _alertManager.CheckActiveAlertForUser(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // בדיקת אזור התרעה לפי מיקום ספציפי
        [HttpGet("check-location")]
        public IActionResult CheckLocation(double lat, double lng)
        {
            try
            {
                // ולידציה
                if (lat == 0 || lng == 0)
                {
                    return BadRequest(new { error = "Invalid coordinates" });
                }

                var result = _alertManager.CheckActiveAlertForLocation(lat, lng);

                // תגובה מובנית - השתמש רק בשדות שקיימים במודל
                return Ok(new
                {
                    isInAlertZone = result.IsInAlertZone,
                    zoneName = result.ZoneName,
                    hasActiveAlert = result.HasActiveAlert,
                    message = result.Message,
                    responseTime = result.ResponseTime,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to check location" });
            }
        }

        // עדכון מיקום משתמש
        [HttpPost("update-location")]
        public IActionResult UpdateUserLocation([FromBody] UpdateLocationRequest request)
        {
            try
            {
                if (request == null || request.UserId == 0)
                {
                    return BadRequest(new { error = "Invalid request data" });
                }

                // עדכן את המיקום
                _alertManager.UpdateUserLocation(
                    request.UserId,
                    request.Latitude,
                    request.Longitude
                );

                // בדוק אם יש התרעה באזור החדש
                var alertResult = _alertManager.CheckActiveAlertForLocation(
                    request.Latitude,
                    request.Longitude
                );

                return Ok(new
                {
                    locationUpdated = true,
                    alertStatus = alertResult
                });
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


    }

    public class LocationCheckRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class UpdateLocationRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}