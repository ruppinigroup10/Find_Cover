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
                message = "User data trensfer successful",
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
        var preferences = FC_Server.Models.UserPreferences.GetUserPreferences(user_id); // קריאה למתודה הסטטית GetUserPreferences במודל UserPreferences
        if (preferences != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט ההעדפות
            {
                message = "Preferences data transfer successful",
                preferences = preferences
            });
        }
        return BadRequest(new { message = "Invalid ID" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }
    // PUT: api/<UserController>/UpdateUserPreferences
    [HttpPost("UpdateUserPreferences")] // שימוש ב-HttpPost עבור עדכון - מקובל יותר מ-HttpPut במקרים מסוימים, אך שקול שימוש ב-HttpPut עבור פעולות עדכון אידמפוטנטיות
    public IActionResult UpdateUserPreferences([FromBody] FC_Server.Models.UserPreferences preferences) // קבלת נתוני העדפות מגוף הבקשה
    {
        var updatedPreferences = FC_Server.Models.UserPreferences.UpdateUserPreferences(preferences.PreferenceId, preferences.UserId, preferences.ShelterType, preferences.AccessibilityNeeded, preferences.NumDefaultPeople, preferences.PetsAllowed, preferences.LastUpdate); // קריאה למתודה הסטטית UpdateUserPreferences במודל UserPreferences
        if (updatedPreferences != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט ההעדפות המעודכן
            {
                message = "Preferences updated successfully",
                preferences = updatedPreferences
            });
        }
        return BadRequest(new { message = "Update failed" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }
    // POST: api/<UserController>/AddPreference     
    [HttpPost("AddPreference")] // שימוש ב-HttpPost עבור הוספה - מתאים לפעולות יצירה
    public IActionResult AddPreference([FromBody] FC_Server.Models.UserPreferences preferences) // קבלת נתוני העדפות מגוף הבקשה
    {
        var newPreference = FC_Server.Models.UserPreferences.AddPreference(preferences.PreferenceId, preferences.UserId, preferences.ShelterType, preferences.AccessibilityNeeded, preferences.NumDefaultPeople, preferences.PetsAllowed); // קריאה למתודה הסטטית AddPreference במודל UserPreferences
        if (newPreference != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט ההעדפות החדש
            {
                message = "Preferences added successfully",
                preferences = newPreference
            });
        }
        return BadRequest(new { message = "Add failed" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }

    // GET: api/<UserController>/GetKnownLocation
    [HttpGet("GetKnownLocation")]
    public IActionResult GetKnownLocation(int user_id)
    {
        var knownLocation = FC_Server.Models.KnownLocation.GetKnownLocation(user_id); // קריאה למתודה הסטטית GetKnownLocation במודל KnownLocation
        if (knownLocation != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט המיקום הידוע
            {
                message = "Known location data transfer successful",
                knownLocation = knownLocation
            });
        }
        return BadRequest(new { message = "Invalid ID" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }
    // PUT: api/<UserController>/UpdateKnownLocation
    [HttpPost("UpdateKnownLocation")] // שימוש ב-HttpPost עבור עדכון - שקול שימוש ב-HttpPut
    public IActionResult UpdateKnownLocation([FromBody] FC_Server.Models.KnownLocation knownLocation) // קבלת נתוני מיקום ידוע מגוף הבקשה
    {
        var updatedKnownLocation = FC_Server.Models.KnownLocation.UpdateKnownLocation(knownLocation.LocationId, knownLocation.UserId, knownLocation.Latitude, knownLocation.Longitude, knownLocation.Radius, knownLocation.Address, knownLocation.LocationName, knownLocation.Nickname, knownLocation.AddedAt); // קריאה למתודה הסטטית UpdateKnownLocation במודל KnownLocation
        if (updatedKnownLocation != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט המיקום הידוע המעודכן
            {
                message = "Known location updated successfully",
                knownLocation = updatedKnownLocation
            });
        }
        return BadRequest(new { message = "Update failed" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }
    // POST: api/<UserController>/AddKnownLocation
    [HttpPost("AddKnownLocation")] // שימוש ב-HttpPost עבור הוספה
    public IActionResult AddKnownLocation([FromBody] FC_Server.Models.KnownLocation knownLocation) // קבלת נתוני מיקום ידוע מגוף הבקשה
    {
        var newKnownLocation = FC_Server.Models.KnownLocation.AddKnownLocation(knownLocation.LocationId, knownLocation.UserId, knownLocation.Latitude, knownLocation.Longitude, knownLocation.Radius, knownLocation.Address, knownLocation.LocationName, knownLocation.Nickname); // קריאה למתודה הסטטית AddKnownLocation במודל KnownLocation
        if (newKnownLocation != null)
        {
            return Ok(new // החזרת תגובת HTTP 200 OK עם הודעה ואובייקט המיקום הידוע החדש
            {
                message = "Known location added successfully",
                knownLocation = newKnownLocation
            });
        }
        return BadRequest(new { message = "Add failed" }); // החזרת תגובת HTTP 400 BadRequest עם הודעת שגיאה
    }
    //--------------------------------------------------------------------------------------------------
    // Default controllers
    //--------------------------------------------------------------------------------------------------

    // GET: api/<UserController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<UserController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<UserController>
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<UserController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<UserController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}




////////////////////////////////////


