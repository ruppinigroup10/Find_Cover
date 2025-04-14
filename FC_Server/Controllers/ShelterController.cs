using Microsoft.AspNetCore.Mvc;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShelterController : ControllerBase
    {
        // POST api/<ShelterController>/AddShelter
        [HttpPost("AddShelter")]
        public IActionResult AddShelter([FromBody] FC_Server.Models.Shelter shelter)
        {
            try
            {
                var newShelter = FC_Server.Models.Shelter.AddShelter(shelter.ShelterType, shelter.Name, shelter.Latitude, shelter.Longitude,
                                shelter.Address, shelter.Capacity, shelter.AdditionalInformation, shelter.ProviderId);

                if (newShelter != null)
                {
                    return Ok(new
                    {
                        message = "Added successfuly",
                        shelter = newShelter
                    });
                }
                return BadRequest(new { message = "Add failed" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("User added this shelter already"))
                {
                    return BadRequest(new { message = "User added this shelter already" });
                }
                throw new Exception("Addition failed");
            }
        }

        // POST api/<ShelterController>/UpdateShelter
        [HttpPost("UpdateShelter")]
        public IActionResult UpdateShelter([FromBody] FC_Server.Models.Shelter shelter)
        {
            try
            {
                var newShelter = FC_Server.Models.Shelter.UpdateShelter(shelter.ShelterId, shelter.ShelterType, shelter.Name, shelter.Latitude, shelter.Longitude,
                                shelter.Address, shelter.Capacity, shelter.AdditionalInformation, shelter.ProviderId);

                if (newShelter != null)
                {
                    return Ok(new
                    {
                        message = "Shelter updated successfuly",
                        shelter = newShelter
                    });
                }
                return BadRequest(new { message = "update failed" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("User added this shelter already"))
                {
                    return BadRequest(new { message = "User added this shelter already" });
                }
                throw new Exception("update failed");
            }
        }

        // GET: api/<ShelterController> getShelter
        [HttpGet("getShelter")]
        public IActionResult getShelter(int shelter_id)
        {
            var shelterData = FC_Server.Models.Shelter.getShelter(shelter_id);

            if (shelterData != null)
            {
                return Ok(new
                {
                    message = "User data trensfer successful",
                    shlter = new
                    {
                        shelter_id = shelterData.ShelterId,
                        provider_id = shelterData.ProviderId,
                        shelter_type = shelterData.ShelterType,
                        name = shelterData.Name,
                        latitude = shelterData.Latitude,
                        longitude = shelterData.Longitude,
                        address = shelterData.Address,
                        capacity = shelterData.Capacity,
                        is_accessible = shelterData.IsAccessible,
                        is_active = shelterData.IsActive,
                        additional_information = shelterData.AdditionalInformation,
                        created_at = shelterData.CreatedAt,
                        last_updated = shelterData.LastUpdated
                    }
                });
            }
            return BadRequest(new { message = "Invalid ID" });
        }

        // POST api/<ShelterController>/shelterActiveStatus
        [HttpPost("shelterActiveStatus")]
        public IActionResult shelterActiveStatus([FromBody] FC_Server.Models.Shelter shelter)
        {
            try
            {
                var newShelter = FC_Server.Models.Shelter.shelterActiveStatus(shelter.ShelterId, shelter.IsActive);

                if (newShelter != null)
                {
                    return Ok(new
                    {
                        message = "Shelter updated successfuly",
                        shelter = newShelter
                    });
                }
                return BadRequest(new { message = "update failed" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid ID"))
                {
                    return BadRequest(new { message = "Invalid ID" });
                }
                throw new Exception("update failed");
            }
        }

        //--------------------------------------------------------------------------------------------------
        // Default controllers
        //--------------------------------------------------------------------------------------------------

        // GET: api/<ShelterController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ShelterController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ShelterController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ShelterController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ShelterController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
