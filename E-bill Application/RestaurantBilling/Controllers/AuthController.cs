using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantBilling.Data;
using RestaurantBilling.DTOs;
using RestaurantBilling.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantBilling.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { error = "Email already exists" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id, GetIpAddress());
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken.Token,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id, GetIpAddress());

            // Remove old inactive tokens
            var expiredTokens = user.RefreshTokens.Where(x => !x.IsActive && x.Created.AddDays(7) <= DateTime.UtcNow).ToList();
            _context.RefreshTokens.RemoveRange(expiredTokens);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken.Token,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return BadRequest(new { error = "Invalid or expired refresh token" });
            }

            // Replace old refresh token with a new one (rotation)
            var newRefreshToken = GenerateRefreshToken(refreshToken.UserId, GetIpAddress());
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = GetIpAddress();
            refreshToken.ReplacedByToken = newRefreshToken.Token;

            var user = refreshToken.User;
            var jwtToken = GenerateJwtToken(user);

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = jwtToken,
                RefreshToken = newRefreshToken.Token,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return BadRequest(new { error = "Token is already inactive" });
            }

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = GetIpAddress();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Token revoked" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("name", user.Name)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(int userId, string ipAddress)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddDays(7), // Long-lived refresh token
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserId = userId
            };
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString();
            else
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
        }
    }
}
