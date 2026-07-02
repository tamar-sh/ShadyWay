using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShadyWay.API.Dtos;
using ShadyWay.Core.Models;
using ShadyWay.Infrastructure;

namespace ShadyWay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ShadyWayDbContext _db;
        private readonly IConfiguration   _config;
        private readonly int              _maxFailedLoginAttempts;
        private readonly int              _lockoutMinutes;

        public AuthController(ShadyWayDbContext db, IConfiguration config)
        {
            _db     = db;
            _config = config;

            _maxFailedLoginAttempts = config.GetValue<int>("Auth:MaxFailedLoginAttempts");
            _lockoutMinutes         = config.GetValue<int>("Auth:LockoutMinutes");
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("כתובת המייל כבר קיימת במערכת.");

            if (await _db.Users.AnyAsync(u => u.IdentityCard == dto.IdentityCard))
                return BadRequest("תעודת הזהות כבר קיימת במערכת.");

            var user = new User
            {
                IdentityCard     = dto.IdentityCard,
                FullName         = dto.FullName,
                Email            = dto.Email,
                PasswordHash     = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                ShadowPreference = dto.ShadowPreference
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(BuildAuthResponse(user));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user != null && user.LockedUntil > DateTime.UtcNow)
            {
                var minutesLeft = Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                return Unauthorized($"החשבון חסום זמנית עקב ניסיונות התחברות כושלים. נסי שוב בעוד {minutesLeft} דקות.");
            }

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= _maxFailedLoginAttempts)
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutMinutes);

                    await _db.SaveChangesAsync();
                }
                return Unauthorized(" אימייל או סיסמה שגויים.");
            }

            // התחברות מוצלחת — איפוס מונה הניסיונות הכושלים
            user.FailedLoginAttempts = 0;
            user.LockedUntil         = null;
            await _db.SaveChangesAsync();

            return Ok(BuildAuthResponse(user));
        }

        private AuthResponseDto BuildAuthResponse(User user) => new()
        {
            Token            = GenerateToken(user),
            FullName         = user.FullName,
            Email            = user.Email,
            ShadowPreference = user.ShadowPreference
        };
        private string GenerateToken(User user)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email,           user.Email),
                new Claim(ClaimTypes.Name,            user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                expires:  DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpiryHours"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
