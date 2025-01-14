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























    }

