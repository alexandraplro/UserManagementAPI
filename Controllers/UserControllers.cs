using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UsersController(IConfiguration config)
        {
            _config = config;
        }
        private static readonly List<User> Users = new List<User>
        {
            new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
            new User { Id = 2, Name = "Bob", Email = "bob@example.com" }
        };

        private static int _nextId = Users.Max(u => u.Id) + 1;

        /// <summary>
        /// Retrieves all users, optionally filtered by search term.
        /// </summary>
        /// <param name="search">Optional search term to filter by name or email.</param>
        /// <returns>A list of users.</returns>
        // 👇 Secured endpoints (token required)
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<User>> GetAllUsers([FromQuery] string? search)
        {
            try
            {
                var results = Users.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    results = results.Where(u =>
                        u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a specific user by their unique ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The user object if found.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<User> GetUserById(int id)
        {
            try
            {
                var user = Users.FirstOrDefault(u => u.Id == id);
                if (user == null) return NotFound($"User with ID {id} not found.");
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new user record.
        /// </summary>
        /// <param name="newUser">The user object to create.</param>
        /// <returns>The newly created user.</returns>
        // 👇 Secured endpoints (token required)
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<User> CreateUser(User newUser)
        {
        
            try
            {
                if (string.IsNullOrWhiteSpace(newUser.Name))
                    return BadRequest("Name is required.");
                if (!Regex.IsMatch(newUser.Name, @"^[a-zA-Z\s'-]+$"))
                    return BadRequest("Name contains invalid characters.");
                if (string.IsNullOrWhiteSpace(newUser.Email) || !new EmailAddressAttribute().IsValid(newUser.Email))
                    return BadRequest("Valid email is required.");
                if (Users.Any(u => u.Email == newUser.Email))
                    return Conflict("A user with this email already exists.");

                newUser.Id = _nextId++;
                Users.Add(newUser);
                return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing user’s details.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="updatedUser">The updated user object.</param>
        // 👇 Secured endpoints (token required)
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateUser(int id, User updatedUser)
        {
            try
            {
                var user = Users.FirstOrDefault(u => u.Id == id);
                if (user == null) return NotFound($"User with ID {id} not found.");

                if (string.IsNullOrWhiteSpace(updatedUser.Name))
                    return BadRequest("Name is required.");
                if (!Regex.IsMatch(updatedUser.Name, @"^[a-zA-Z\s'-]+$"))
                    return BadRequest("Name contains invalid characters.");
                if (string.IsNullOrWhiteSpace(updatedUser.Email) || !new EmailAddressAttribute().IsValid(updatedUser.Email))
                    return BadRequest("Valid email is required.");

                user.Name = updatedUser.Name;
                user.Email = updatedUser.Email;
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a user by their unique ID.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        // 👇 Secured endpoints (token required)
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var user = Users.FirstOrDefault(u => u.Id == id);
                if (user == null) return NotFound($"User with ID {id} not found.");

                Users.Remove(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // 👇 Public endpoint (no token required)
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] UserLoginRequest request)
        {
            try
            {
                if (request.Username == "admin" && request.Password == "password")
                
                {
                    var tokenHandler = new JwtSecurityTokenHandler();

                    // 👇 Pull values from appsettings.json
                    var keyString = _config["Jwt:Key"] 
                        ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
                    var key = Encoding.ASCII.GetBytes(keyString);

                    var issuer = _config["Jwt:Issuer"] 
                        ?? throw new InvalidOperationException("JWT Issuer is missing in configuration.");
                    var audience = _config["Jwt:Audience"] 
                        ?? throw new InvalidOperationException("JWT Audience is missing in configuration.");


                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, request.Username) }),
                        Expires = DateTime.UtcNow.AddHours(1),
                        SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                        ),
                        Issuer = issuer,
                        Audience = audience
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var jwt = tokenHandler.WriteToken(token);

                return Ok(new { token = jwt });
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
            return StatusCode(500, new { error = ex.Message });
            }
        
        //For testing purposes only
        /*  }
        [HttpGet("error")]
        public IActionResult GetError()
        {
            throw new Exception("Forced error for testing");
        } */
        }
    }

    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

  