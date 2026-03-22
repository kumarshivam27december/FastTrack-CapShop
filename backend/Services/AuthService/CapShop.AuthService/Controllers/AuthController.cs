using CapShop.AuthService.Data;
using CapShop.AuthService.DTOs.Auth;
using CapShop.AuthService.Models;
using CapShop.AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;


namespace CapShop.AuthService.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _db;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(AuthDbContext db, IJwtTokenService jwtTokenService)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "AuthService", status = "Healthy" });


        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email)|| string.IsNullOrWhiteSpace(request.Password))
            {
                if (string.IsNullOrEmpty(request.Email)) 
                { 
                    return BadRequest("Email is required"); 
                }
                else
                {

                return BadRequest("Password are required");
                }
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var exists = await _db.Users.AnyAsync(x => x.Email == email);
            if (exists)
            {
                return Conflict("Email already exists");
            }

            var customerRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == "Customer");
            if(customerRole is null)
            {
                return StatusCode(500, "Default role customer not found");
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };

            user.UserRoles.Add(new UserRole { User=user,Role=customerRole});

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Signup successful." });
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users
                            .Include(u=>u.UserRoles)
                            .ThenInclude(ur=>ur.Role)
                            .FirstOrDefaultAsync(x => x.Email == email);


            if(user is null || !user.IsActive)
            {
                return Unauthorized("Invalid credentials.");
            }

            var passValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passValid)
            {
                return Unauthorized("Invalid credentials.");
            }

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Role = roles.FirstOrDefault() ?? "Customer",
                Email = user.Email
            });
        }



        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var email = User.Identity?.Name ?? string.Empty;
            var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            return Ok(new { email, roles });
        }


    }
}
