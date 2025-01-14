using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketApi.Models;
using System.Threading.Tasks;

namespace OnlineMarketApi.Controllers
{

    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An error occurred while processing your request."
            }.ToString());
        }
    }


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

        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.FullName) ||
                string.IsNullOrWhiteSpace(userDto.Email) || string.IsNullOrWhiteSpace(userDto.Password))
            {
                return BadRequest("All fields are required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null)
                return BadRequest("Email is already in use.");

            var user = new User
            {
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Address = userDto.Address,
                BirthDate = userDto.BirthDate,
                Gender = userDto.Gender,
                PhoneNumber = userDto.PhoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

    }


    [HttpPost("login")]
        [SwaggerOperation(Summary = "Login an existing user", Description = "Authenticate user and return a JWT token.")]
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

        [HttpGet("users")]
        [SwaggerOperation(Summary = "Get a paginated list of users", Description = "Returns users with pagination.")]
        public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var users = await _context.Users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Clear client-side data (example: cookies and storage)
            Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\"");
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var profile = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Address,
                user.BirthDate,
                user.Gender,
                user.PhoneNumber
            };

            return Ok(profile);
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
