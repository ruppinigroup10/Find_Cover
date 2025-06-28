using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertController : ControllerBase
    {
        [HttpGet("fetch")]
        public async Task<IActionResult> FetchAndSaveAlerts()
        {
            DBservicesAlert db = new DBservicesAlert();
            var alerts = await db.GetAlertsFromApi();
            db.SaveAlertsToDb(alerts);
            return Ok(new { message = $"{alerts.Count} alerts processed" });
        }

        [HttpPost]
        [Route("api/alert/simulate")]
        public IActionResult SimulateFakeAlert()
        {
            DBservicesAlert db = new DBservicesAlert();

            Alert fakeAlert = new Alert
            {
                AlertTime = DateTime.UtcNow,
                AlertType = "Simulated",
                cities = new List<string> { "באר שבע - דרום", "באר שבע - מזרח", "באר שבע - מערב", "באר שבע - צפון" },
                IsActive = true
            };

            db.SaveAlertsToDb(new List<Alert> { fakeAlert });

            return Ok("Simulated alert sent successfully.");
        }


    }
}
