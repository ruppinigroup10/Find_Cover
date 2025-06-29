using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertController : ControllerBase
    {
        private readonly FirebaseNotificationSender _fcmSender;
        private readonly DBservicesAlert _db;

        public AlertController(FirebaseNotificationSender fcmSender, DBservicesAlert db)
        {
            _fcmSender = fcmSender;
            _db = db;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> FetchAndSaveAlerts()
        {
            DBservicesAlert db = new DBservicesAlert();
            var alerts = await db.GetAlertsFromApi();
            db.SaveAlertsToDb(alerts);
            return Ok(new { message = $"{alerts.Count} alerts processed" });
        }
        [HttpPost]
        [Route("simulate")]
        public async Task<IActionResult> SimulateFakeAlert()
        {
            try
            {
                Alert fakeAlert = new Alert
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    threat = 1,
                    cities = new List<string> { "באר שבע - דרום", "באר שבע - מזרח", "באר שבע - מערב", "באר שבע - צפון" },
                    IsActive = true
                };

                _db.SaveAlertsToDb(new List<Alert> { fakeAlert });

                await _fcmSender.SendNotificationAsync(
    data: new Dictionary<string, string>
    {
        { "type", "trigger_location" },
        { "code", "FIND_SHELTER" }
    },
    topic: "alerts"
);


                return Ok("Simulated alert sent and notification pushed.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה פנימית: {ex.Message}");
            }
        }


        /*  [HttpPost]
          [Route("simulate")]
          public IActionResult SimulateFakeAlert()
          {
              DBservicesAlert db = new DBservicesAlert();

              Alert fakeAlert = new Alert
              {
                  time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                  threat = 1,
                  cities = new List<string> { "באר שבע - דרום", "באר שבע - מזרח", "באר שבע - מערב", "באר שבע - צפון" },
                  IsActive = true
              };

              db.SaveAlertsToDb(new List<Alert> { fakeAlert });


              return Ok("Simulated alert sent successfully.");
          }*/


    }
}
