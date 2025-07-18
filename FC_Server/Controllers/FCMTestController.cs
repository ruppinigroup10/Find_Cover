using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FCMTestController : ControllerBase
    {
        private readonly FirebaseNotificationSender _notificationSender;
        private readonly ILogger<FCMTestController> _logger;

        public FCMTestController(FirebaseNotificationSender notificationSender, ILogger<FCMTestController> logger)
        {
            _notificationSender = notificationSender;
            _logger = logger;
        }

        /// <summary>
        /// Test FCM with minimal data - just to see if FCM works
        /// </summary>
        [HttpPost("test-basic")]
        public async Task<IActionResult> TestBasicFCM()
        {
            try
            {
                await _notificationSender.SendNotificationAsync(
                    title: "Test Notification",
                    body: "This is a test message",
                    data: new Dictionary<string, string>
                    {
                        { "test", "true" },
                        { "timestamp", DateTime.Now.ToString("O") }
                    },
                    topic: "alerts"  // Using general alerts topic for testing
                );

                _logger.LogInformation("Basic FCM test sent successfully");
                return Ok(new { success = true, message = "Basic FCM notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send basic FCM test");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Test FCM with shelter allocation data - exact same as in your code
        /// </summary>
        [HttpPost("test-shelter-allocation")]
        public async Task<IActionResult> TestShelterAllocationFCM([FromBody] TestShelterRequest request)
        {
            try
            {
                // Using test data or data from request
                var data = new Dictionary<string, string>
                {
                    { "type", "shelter_allocation" },
                    { "shelter_id", request?.ShelterId ?? "123" },
                    { "latitude", request?.Latitude?.ToString(CultureInfo.InvariantCulture) ?? "31.2589" },
                    { "longitude", request?.Longitude?.ToString(CultureInfo.InvariantCulture) ?? "34.7998" },
                    { "address", request?.Address ?? "רחוב הבדיקה 1, באר שבע" },
                    { "name", request?.ShelterName ?? "מרחב מוגן לבדיקה" }
                };

                await _notificationSender.SendNotificationAsync(
                    title: "נמצא עבורך מרחב מוגן",
                    body: "לחץ לניווט",
                    data: data,
                    topic: $"user_{request?.UserId ?? 1}"  // Default to user_1 for testing
                );

                _logger.LogInformation("Shelter allocation FCM test sent successfully");
                return Ok(new
                {
                    success = true,
                    message = "Shelter allocation FCM notification sent successfully",
                    sentData = data,
                    topic = $"user_{request?.UserId ?? 1}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send shelter allocation FCM test");
                return StatusCode(500, new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Test FCM to specific user topic
        /// </summary>
        [HttpPost("test-user-topic/{userId}")]
        public async Task<IActionResult> TestUserTopicFCM(int userId)
        {
            try
            {
                await _notificationSender.SendNotificationAsync(
                    title: "Test for User",
                    body: $"This is a test message for user {userId}",
                    data: new Dictionary<string, string>
                    {
                        { "type", "test_user_specific" },
                        { "user_id", userId.ToString() },
                        { "timestamp", DateTime.Now.ToString("O") }
                    },
                    topic: $"user_{userId}"
                );

                _logger.LogInformation("User topic FCM test sent successfully to user_{UserId}", userId);
                return Ok(new { success = true, message = $"FCM notification sent to topic: user_{userId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send user topic FCM test for user {UserId}", userId);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Test FCM with only data (no notification body) - for background handling
        /// </summary>
        [HttpPost("test-data-only")]
        public async Task<IActionResult> TestDataOnlyFCM()
        {
            try
            {
                await _notificationSender.SendNotificationAsync(
                    title: null,  // No title
                    body: null,   // No body
                    data: new Dictionary<string, string>
                    {
                        { "type", "silent_test" },
                        { "action", "background_process" },
                        { "timestamp", DateTime.Now.ToString("O") }
                    },
                    topic: "alerts"
                );

                _logger.LogInformation("Data-only FCM test sent successfully");
                return Ok(new { success = true, message = "Data-only FCM notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send data-only FCM test");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    // Request model for testing
    public class TestShelterRequest
    {
        public int? UserId { get; set; }
        public string ShelterId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Address { get; set; }
        public string ShelterName { get; set; }
    }
}