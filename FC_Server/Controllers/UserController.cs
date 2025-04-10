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

        // GET api/<UserController>/preferences
        [HttpGet("preferences")]
        public IActionResult GetPreferences(int user_id) //TODO: CHECK AUTHENTICATION
        {
            var preferences = FC_Server.Models.UserPreferences.GetPreferences(user_id);
            if (preferences != null)
            {
                return Ok(new
                {
                    message = "Preferences data transfer successful",
                    preferences = preferences
                });
            }
            return BadRequest(new { message = "Invalid ID" });
        }
<<<<<<< Updated upstream
        // POST api/<UserController>/preferences
        [HttpPost("preferences")]
        public IActionResult UpdatePreferences([FromBody] FC_Server.Models.UserPreferences preferences)//todo : check authentication
=======


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
>>>>>>> Stashed changes
        {
            var updatedPreferences = FC_Server.Models.UserPreferences.UpdatePreferences(preferences.Id, preferences);
            if (updatedPreferences != null)
            {
                return Ok(new
                {
                    message = "Preferences updated successfully",
                    preferences = updatedPreferences
                });
            }
            return BadRequest(new { message = "Update failed" });
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
