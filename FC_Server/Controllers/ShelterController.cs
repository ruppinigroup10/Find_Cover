using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using FC_Server.DAL;


namespace FC_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShelterController : ControllerBase
    {

        private readonly IGoogleMapsService _googleMapsService;

        // Add constructor to inject GoogleMapsService
        public ShelterController(IGoogleMapsService googleMapsService)
        {
            _googleMapsService = googleMapsService;
        }

        // POST api/<ShelterController>/AddShelter
        // [HttpPost("AddShelter")]
        // public IActionResult AddShelter([FromBody] FC_Server.Models.Shelter shelter)
        // {
        //     try
        //     {
        //         var newShelter = FC_Server.Models.Shelter.AddShelter("", shelter.Name, 0, 0,
        //                         shelter.Address, shelter.Capacity, shelter.IsAccessible, shelter.PetsFriendly,
        //                         shelter.AdditionalInformation, shelter.ProviderId);

        //         if (newShelter != null)
        //         {
        //             return Ok(new
        //             {
        //                 message = "Added successfuly",
        //                 shelter = newShelter
        //             });
        //         }
        //         return BadRequest(new { message = "Add failed" });
        //     }
        //     catch (Exception ex)
        //     {
        //         if (ex.Message.Contains("User added this shelter already"))
        //         {
        //             return BadRequest(new { message = "User added this shelter already" });
        //         }
        //         if (ex.Message.Contains("User not exist"))
        //         {
        //             return BadRequest(new { message = "User not exist" });
        //         }
        //         throw new Exception("Addition failed");
        //     }
        // }


        // POST api/<ShelterController>/AddShelter
        [HttpPost("AddShelter")]
        public async Task<IActionResult> AddShelter([FromBody] FC_Server.Models.Shelter shelter)
        {
            try
            {
                // Handle nullable coordinates - use 0 as default if null
                float latitude = shelter.Latitude;
                float longitude = shelter.Longitude;

                // Note the await and passing googleMapsService
                var newShelter = await FC_Server.Models.Shelter.AddShelter(
                    "",
                    shelter.Name,
                    latitude,
                    longitude,
                    shelter.Address,
                    shelter.Capacity,
                    shelter.IsAccessible,
                    shelter.PetsFriendly,
                    shelter.AdditionalInformation,
                    shelter.ProviderId,
                    _googleMapsService
                );

                if (newShelter != null)
                {
                    return Ok(new
                    {
                        message = "Added successfully",
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

        // Direct geocoding
        [HttpGet("geocode")]
        public async Task<IActionResult> GeocodeAddress([FromQuery] string address)
        {
            var coordinates = await _googleMapsService.GetCoordinatesFromAddressAsync(address);
            if (coordinates.HasValue)
            {
                return Ok(new
                {
                    success = true,
                    latitude = coordinates.Value.latitude,
                    longitude = coordinates.Value.longitude
                });
            }
            return NotFound(new { success = false, message = "Could not geocode address" });
        }


        // Reverse geocoding endpoint
        [HttpGet("reverse-geocode")]
        public async Task<IActionResult> ReverseGeocode([FromQuery] float latitude, [FromQuery] float longitude)
        {
            var address = await _googleMapsService.GetAddressFromCoordinatesAsync(latitude, longitude);
            if (!string.IsNullOrEmpty(address))
            {
                return Ok(new { success = true, address = address });
            }
            return NotFound(new { success = false, message = "Could not find address" });
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
    }
}
