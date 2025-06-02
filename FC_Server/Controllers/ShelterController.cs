using FC_Server.Models;
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
                var newShelter = FC_Server.Models.Shelter.AddShelter("", shelter.Name, 0, 0,
                                shelter.Address, shelter.Capacity, shelter.IsAccessible, shelter.PetsFriendly,
                                shelter.AdditionalInformation, shelter.ProviderId);

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
                if (ex.Message.Contains("User not exist"))
                {
                    return BadRequest(new { message = "User not exist" });
                }
                throw new Exception("Addition failed");
            }
        }

        // PUT api/<ShelterController>/UpdateShelter
        [HttpPut("UpdateShelter")]
        public IActionResult UpdateShelter([FromBody] FC_Server.Models.Shelter shelter)
        {
            try
            {
                var newShelter = FC_Server.Models.Shelter.UpdateShelter(shelter.ShelterId, shelter.ShelterType, shelter.Name, shelter.Latitude, shelter.Longitude,
                                shelter.Address, shelter.Capacity, shelter.AdditionalInformation, shelter.ProviderId, shelter.PetsFriendly, shelter.IsAccessible, shelter.IsActive);

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
                        pets_friendly = shelterData.PetsFriendly,
                        is_active = shelterData.IsActive,
                        additional_information = shelterData.AdditionalInformation,
                        created_at = shelterData.CreatedAt,
                        last_updated = shelterData.LastUpdated
                    }
                });
            }
            return BadRequest(new { message = "Invalid ID" });
        }

        // GET: api/<ShelterController> getMyShelter
        [HttpGet("getMyShelter")]
        public IActionResult getMyShelter(int provider_id)
        {
            List<Shelter> sheltersData = FC_Server.Models.Shelter.getMyShelters(provider_id);

            if (sheltersData != null && sheltersData.Count > 0)
            {
                return Ok(new
                {
                    message = "User data transfer successful",
                    shelters = sheltersData.Select(shelter => new
                    {
                        shelter_id = shelter.ShelterId,
                        provider_id = shelter.ProviderId,
                        shelter_type = shelter.ShelterType,
                        name = shelter.Name,
                        latitude = shelter.Latitude,
                        longitude = shelter.Longitude,
                        address = shelter.Address,
                        capacity = shelter.Capacity,
                        is_accessible = shelter.IsAccessible,
                        is_active = shelter.IsActive,
                        additional_information = shelter.AdditionalInformation,
                        created_at = shelter.CreatedAt,
                        last_updated = shelter.LastUpdated
                    })
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

        [HttpDelete("DeleteShelter")]
        public IActionResult DeleteShelter(int shelter_id, int provider_id)
        {
            try
            {
                Shelter.DeleteShelter(shelter_id, provider_id);
                return Ok(new { message = "Shelter deleted successfully" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Shelter not found"))
                {
                    return BadRequest(new { message = "Shelter not found" });
                }
                if (ex.Message.Contains("User not found"))
                {
                    return BadRequest(new { message = "User not found" });
                }
                if (ex.Message.Contains("Shelter not owned by this user"))
                {
                    return BadRequest(new { message = "Shelter not owned by this user" });
                }

                return BadRequest(new { message = "Deletion failed" });
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
