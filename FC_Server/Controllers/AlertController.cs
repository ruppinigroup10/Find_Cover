using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
    
        // GET: api/<AlertController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<AlertController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AlertController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AlertController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AlertController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
