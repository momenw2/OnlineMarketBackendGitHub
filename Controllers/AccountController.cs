using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using OnlineMarketApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;


namespace OnlineMarketApi.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("User data is required");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null)
            {
                return BadRequest("Email is already in use.");
            }

            var user = new User
            {
                FullName = userDto.FullName,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password), // Hash the password
                Email = userDto.Email,
                Address = userDto.Address,
                BirthDate = userDto.BirthDate,
                Gender = userDto.Gender,
                PhoneNumber = userDto.PhoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateToken(user); // Call the token generation method

            return Ok(new { Token = token });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
            {
                return Unauthorized();
            }

            var token = GenerateToken(user); // Return token on successful login
            return Ok(new { Token = token });
        }




        private string GenerateToken(User user)
        {
            var key = Encoding.UTF8.GetBytes("your-super-secret-key-that-is-long-enough");
            var signingKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication",
            Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: "your-issuer",
                audience: "your-audience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }




        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear the JWT token from client-side
            Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\"");

            return Ok(new { status = (string)null, message = "Logged Out" });
        }




        [HttpGet("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                if (email == null)
                {
                    var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                    Console.WriteLine($"Authorization Header: {authHeader}");
                    return Unauthorized(new { status = "Unauthorized", message = "Invalid token or user not found" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return Unauthorized(new { status = "Unauthorized", message = "User not found" });
                }

                var profile = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    birthDate = user.BirthDate.ToString("o"),
                    gender = user.Gender,
                    address = user.Address,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber
                };

                if (Request.Headers["Accept"].ToString().Contains("text/plain"))
                {
                    var profileText = System.Text.Json.JsonSerializer.Serialize(profile);
                    return Content(profileText, "text/plain");
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", message = ex.Message });
            }
        }


        [HttpPut("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (email == null)
                {
                    return Unauthorized(new { status = "Error", message = "Invalid token" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return NotFound(new { status = "Error", message = "User not found" });
                }

                // Validate gender
                if (!string.IsNullOrEmpty(request.Gender) &&
                    !new[] { "Male", "Female" }.Contains(request.Gender))
                {
                    return BadRequest(new { status = "Error", message = "Invalid gender value" });
                }

                // Update user properties
                user.FullName = request.FullName;
                user.BirthDate = request.BirthDate;
                user.Gender = request.Gender;
                user.Address = request.Address;
                user.PhoneNumber = request.PhoneNumber;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", message = ex.Message });
            }
        }



    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class ProfileUpdateRequest
    {
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
    }
}

