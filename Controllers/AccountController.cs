using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketApi.Models;
using System.Threading.Tasks;

namespace OnlineMarketApi.Controllers
{
    public partial class AccountController : ControllerBase
    {

        private string GenerateToken(User user)
        {
            var key = Encoding.UTF8.GetBytes("your-secret-key-that-is-long-enough");
            var signingKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
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
    }


    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }


        private bool IsPasswordStrong(string password)
        {
            // Password must be at least 8 characters, contain at least one uppercase letter,
            // one lowercase letter, one digit, and one special character.
            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(ch => "!@#$%^&*()".Contains(ch));

            return password.Length >= 8 && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("User data is required");
            }

            if (!IsPasswordStrong(userDto.Password))
            {
                return BadRequest("Password does not meet complexity requirements.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null)
            {
                return BadRequest("Email is already in use.");
            }

            var user = new User
            {
                FullName = userDto.FullName,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Email = userDto.Email,
                Address = userDto.Address,
                BirthDate = userDto.BirthDate,
                Gender = userDto.Gender,
                PhoneNumber = userDto.PhoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful" });
        }
    }


    [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("Login data is required");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            var token = GenerateToken(user);
            return Ok(new { Token = token });
        }

public class UserLoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }


    public class UserRegisterDto
    {
        public string FullName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
    }
}
