using System.Linq.Expressions;
using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FC_Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    // POST api/<UserController>/Register  
    [HttpPost("Register")]
    public IActionResult Register([FromBody] User user)
    {
        try
        {
            var newUser = FC_Server.Models.User.Register(user.Username, user.PasswordHash, user.Email, user.PhoneNumber);

            if (newUser != null)
            {
                return Ok(new
                {
                    message = "Registration successful",
                    user = newUser
                });
            }
            return BadRequest(new { message = "Registration failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Email already exists"))
            {
                return BadRequest(new { message = "Email already exists" });
            }
            if (ex.Message.Contains("Phone already exists"))
            {
                return BadRequest(new { message = "Phone already exists" });
            }
            return BadRequest(new { message = "Registration failed" });
        }
    }

    // PUT api/<UserController>/UpdateUser  
    [HttpPut("UpdateUser")]
    public IActionResult UpdateUser([FromBody] User user)
    {
        try
        {
            var updatedUser = FC_Server.Models.User.UpdateUser(user.UserId, user.Username, user.PasswordHash, user.Email, user.PhoneNumber);

            if (updatedUser != null)
            {
                return Ok(new
                {
                    message = "Profile updated successfully",
                    user = updatedUser
                });
            }
            return BadRequest(new { message = "Update failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Email already exists"))
            {
                return BadRequest(new { message = "Email already exists" });
            }
            if (ex.Message.Contains("Phone already exists"))
            {
                return BadRequest(new { message = "Phone already exists" });
            }
            return BadRequest(new { message = "Update failed" });
        }
    }

    //Post api/<UserController>/Login  
    [HttpPost("Login")]
    public IActionResult Login([FromBody] User user)
    {
        try
        {
            var userLoging = FC_Server.Models.User.Login(user.Email, user.PasswordHash);

            if (userLoging != null)
            {
                return Ok(new
                {
                    message = "Login successful",
                    user = new
                    {
                        user_id = userLoging.UserId,
                        username = userLoging.Username,
                        email = userLoging.Email,
                        phone_number = userLoging.PhoneNumber,
                        is_active = userLoging.IsActive,
                        is_provider = userLoging.IsProvider
                    }
                });
            }
            return BadRequest(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Account is not active"))
            {
                return BadRequest(new { message = "Account is not active" });
            }
            return BadRequest(new { message = "Login failed" });
        }
    }

    // GET: api/<UsersController> getUser  
    [HttpGet("getUser")]
    public IActionResult getUser(int user_id)
    {
        var userData = FC_Server.Models.User.getUser(user_id);

        if (userData != null)
        {
            return Ok(new
            {
                message = "User data transfer successful",
                user = new
                {
                    user_id = userData.UserId,
                    username = userData.Username,
                    email = userData.Email,
                    phone_number = userData.PhoneNumber,
                    is_active = userData.IsActive,
                    is_provider = userData.IsProvider
                }
            });
        }
        return BadRequest(new { message = "Invalid ID" });
    }

    // GET: api/<UserController>/GetUserPreferences  
    [HttpGet("GetUserPreferences")]
    public IActionResult GetUserPreferences(int user_id)
    {
        try
        {
            var preferences = FC_Server.Models.UserPreferences.GetUserPreferences(user_id);

            if (preferences != null)
            {
                return Ok(new
                {
                    message = "Preferences data transfer successful",
                    preferences = preferences
                });
            }

            return BadRequest(new { message = "No preferences found" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("No preferences found for this user"))
            {
                return BadRequest(new { message = "No preferences found for this user" });
            }
            if (ex.Message.Contains("Invalid ID"))
            {
                return BadRequest(new { message = "Invalid ID" });
            }
            return BadRequest(new { message = "Failed to retrieve preferences" });
        }
    }

    // PUT: api/<UserController>/UpdateUserPreferences  
    [HttpPut("UpdateUserPreferences")]
    public IActionResult UpdateUserPreferences([FromBody] FC_Server.Models.UserPreferences preferences)
    {
        try
        {
            var updatedPreferences = FC_Server.Models.UserPreferences.UpdateUserPreferences(
                //preferences.PreferenceId,
                preferences.UserId, preferences.ShelterType, preferences.AccessibilityNeeded, preferences.NumDefaultPeople, preferences.PetsAllowed);
            if (updatedPreferences != null)
            {
                return Ok(new
                {
                    message = "Preferences added successfully",
                    preferences = updatedPreferences
                });
            }
            return BadRequest(new { message = "Add failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("User not found"))
            {
                return BadRequest(new { message = "User not found" });
            }
            if (ex.Message.Contains("preference not found for this user"))
            {
                return BadRequest(new { message = "preference not found for this user" });
            }

            return BadRequest(new { message = "update failed" });
        }

    }

    // POST: api/<UserController>/AddPreference  
    [HttpPost("AddPreference")]
    public IActionResult AddPreference([FromBody] FC_Server.Models.UserPreferences preferences)
    {
        try
        {
            var newPreference = FC_Server.Models.UserPreferences.AddPreference(preferences.UserId, preferences.ShelterType, preferences.AccessibilityNeeded, preferences.NumDefaultPeople, preferences.PetsAllowed);
            if (newPreference != null)
            {
                return Ok(new
                {
                    message = "Preferences added successfully",
                    preferences = newPreference
                });
            }
            return BadRequest(new { message = "Add failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                return BadRequest(new { message = "Invalid ID" });
            }
            if (ex.Message.Contains("User added Preference already"))
            {
                return BadRequest(new { message = "User already added preferences" });
            }

            return BadRequest(new { message = "Add failed" });
        }
    }

    // GET: api/<UserController>/GetKnownLocation  
    [HttpGet("GetKnownLocation")]
    public IActionResult GetKnownLocation(int location_id)
    {
        var knownLocation = FC_Server.Models.KnownLocation.GetKnownLocation(location_id);

        if (knownLocation != null)
        {
            return Ok(new
            {
                message = "Known location data transfer successful",
                knownLocation = new
                { 
                    location_id = knownLocation.LocationId,
                    user_id = knownLocation.UserId,
                    latitude = knownLocation.Latitude,
                    longitude = knownLocation.Longitude,
                    radius = knownLocation.Radius,
                    address = knownLocation.Address,
                    location_name = knownLocation.LocationName,
                    created_at = knownLocation.CreatedAt
                }
            });
        }
        return BadRequest(new { message = "Invalid ID" });
    }

    // GET: api/<UserController>/GetMyKnownLocations  
    [HttpGet("GetMyKnownLocations")]
    public IActionResult GetMyKnownLocations(int user_id)
    {
        var knownLocations = FC_Server.Models.KnownLocation.GetMyKnownLocations(user_id);

        if (knownLocations != null && knownLocations.Count > 0)
        {
            return Ok(new
            {
                message = "Known locations data transfer successful",
                knownLocations = knownLocations
            });
        }

        return BadRequest(new { message = "No known locations found for this user" });
    }

    // PUT: api/<UserController>/UpdateKnownLocation  
    [HttpPut("UpdateKnownLocation")]
    public IActionResult UpdateKnownLocation([FromBody] FC_Server.Models.KnownLocation knownLocation)
    {
        try
        {
            if (knownLocation.CreatedAt == null)
            {
                return BadRequest(new { message = "CreatedAt cannot be null" });
            }

            var updatedKnownLocation = FC_Server.Models.KnownLocation.UpdateKnownLocation(
                knownLocation.LocationId,
                knownLocation.UserId,
                knownLocation.Latitude,
                knownLocation.Longitude,
                knownLocation.Radius,
                knownLocation.Address,
                knownLocation.LocationName
            );

            if (updatedKnownLocation != null)
            {
                return Ok(new
                {
                    message = "Known location updated successfully",
                    knownLocation = updatedKnownLocation
                });
            }
            return BadRequest(new { message = "Update failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                return BadRequest(new { message = "Invalid ID" });
            }
            if (ex.Message.Contains("Location not found for this user"))
            {
                return BadRequest(new { message = "Location not found for this user" });
            }
            return BadRequest(new { message = "Update failed" });
        }
    }

    // POST: api/<UserController>/AddKnownLocation  
    [HttpPost("AddKnownLocation")]
    public IActionResult AddKnownLocation([FromBody] FC_Server.Models.KnownLocation knownLocation)
    {
        try
        {
            var newKnownLocation = FC_Server.Models.KnownLocation.AddKnownLocation(knownLocation.UserId, knownLocation.Latitude, knownLocation.Longitude, knownLocation.Radius, knownLocation.Address, knownLocation.LocationName);
            if (newKnownLocation != null)
            {
                return Ok(new
                {
                    message = "Known location added successfully",
                    knownLocation = newKnownLocation
                });
            }
            return BadRequest(new { message = "Add failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                return BadRequest(new { message = "Invalid ID" });
            }
            if (ex.Message.Contains("User added this known location already"))
            {
                return BadRequest(new { message = "User added this known location already" });
            }
            return BadRequest(new { message = "Addition failed" });
        }
    }

    [HttpDelete("DeleteKnownLocation")]
    public IActionResult DeleteKnownLocation(int location_id, int user_id)
    {
        try
        {
            bool result = FC_Server.Models.KnownLocation.DeleteKnownLocation(location_id, user_id);
            if (result)
            {
                return Ok(new { message = "Known location deleted successfully" });
            }
            return BadRequest(new { message = "Delete failed" });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
                return BadRequest(new { message = "Invalid ID" });
            if (ex.Message.Contains("Location not found"))
                return BadRequest(new { message = "Location not found for this user" });

            return BadRequest(new { message = "Delete failed" });
        }
    }

    // Default controllers  
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }  
}

