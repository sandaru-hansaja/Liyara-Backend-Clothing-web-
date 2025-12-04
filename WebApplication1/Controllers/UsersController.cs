////using BCrypt.Net;

////using Microsoft.AspNetCore.Mvc;
////using Microsoft.EntityFrameworkCore;
////using Microsoft.Extensions.Configuration;
////using Microsoft.IdentityModel.Tokens;
////using System;
////using System.Collections.Generic;
////using System.IdentityModel.Tokens.Jwt;
////using System.Linq;
////using System.Security.Claims;
////using System.Text;
////using System.Threading.Tasks;
////using WebApplication1.Models;

////namespace WebApplication1.Controllers
////{
////    [Route("api/[controller]")]
////    [ApiController]
////    public class UsersController : ControllerBase
////    {
////        private readonly LiaraDbContext _dbContext;
////        private readonly IConfiguration _configuration;

////        public UsersController(LiaraDbContext dbContext, IConfiguration configuration)
////        {
////            _dbContext = dbContext;
////            _configuration = configuration;
////        }



////        [HttpPost("register")]
////        public async Task<IActionResult> Register([FromBody] registerModel request)
////        {
////            if (!ModelState.IsValid)
////                return BadRequest(ModelState);

////            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
////            if (existingUser != null)
////                return Conflict("Email already exists.");

////            var user = new Users
////            {
////                Username = request.Username,
////                Email = request.Email,
////                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
////                Role = "customer" //  Default role set here
////            };

////            _dbContext.Users.Add(user);
////            await _dbContext.SaveChangesAsync();

////            return Ok(new
////            {
////                message = "User registered successfully.",
////                user.UserID,
////                user.Username,
////                user.Email
////            });
////        }



////        // ✅ Login User
////        [HttpPost("login")]
////        public async Task<IActionResult> Login([FromBody] WebApplication1.Models.LoginDto request)
////        {
////            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
////                return BadRequest("Email and password are required.");

////            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
////            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
////            {
////                return Unauthorized("Invalid email or password.");
////            }

////            var token = GenerateJwtToken(user);

////            return Ok(new
////            {
////                message = "Login successful",
////                token,
////                user = new
////                {
////                    user.UserID,
////                    user.Username,
////                    user.Email,
////                    user.Role
////                }
////            });
////        }

////        // ✅ Reset Password
////        [HttpPost("reset-password")]
////        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
////        {
////            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.NewPassword))
////                return BadRequest("Email and new password are required.");

////            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
////            if (user == null)
////                return NotFound("User not found.");

////            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
////            _dbContext.Users.Update(user);
////            await _dbContext.SaveChangesAsync();

////            return Ok(new { message = "Password reset successfully." });
////        }
////        // PUT: api/Users/UpdateRole/5
////        [HttpPut("UpdateRole/{id}")]
////        public IActionResult UpdateUserRole(int id, [FromBody] string role)
////        {
////            var user = _dbContext.Users.FirstOrDefault(u => u.UserID == id);
////            if (user == null)
////            {
////                return NotFound("User not found.");
////            }

////            if (string.IsNullOrEmpty(role))
////            {
////                return BadRequest("Role is required.");
////            }

////            user.Role = role;
////            _dbContext.SaveChanges();

////            return Ok(new { message = "User role updated successfully." });
////        }

////        // ✅ Delete User
////        [HttpDelete("{id}")]
////        public async Task<IActionResult> DeleteUser(int id)
////        {
////            var user = await _dbContext.Users.FindAsync(id);
////            if (user == null) return NotFound("User not found.");

////            _dbContext.Users.Remove(user);
////            await _dbContext.SaveChangesAsync();

////            return Ok("User deleted successfully.");
////        }

////        // ✅ Get All Users
////        [HttpGet]
////        public async Task<IActionResult> GetAllUsers()
////        {
////            var users = await _dbContext.Users
////                .Select(u => new { u.UserID, u.Username, u.Email, u.Role })
////                .ToListAsync();

////            return Ok(users);
////        }

////        // 🔐 JWT Generator
////        private string GenerateJwtToken(Users user)
////        {
////            var jwtKey = _configuration["JwtSettings:Key"];
////            var issuer = _configuration["JwtSettings:Issuer"];
////            var audience = _configuration["JwtSettings:Audience"];

////            if (string.IsNullOrEmpty(jwtKey))
////                throw new Exception("JWT key is not configured.");

////            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
////            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

////            var claims = new[]
////            {
////                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
////                new Claim(ClaimTypes.Name, user.Username),
////                new Claim(ClaimTypes.Email, user.Email),
////                new Claim(ClaimTypes.Role, user.Role ?? "User")
////             };

////            var token = new JwtSecurityToken(
////                issuer: issuer,
////                audience: audience,
////                claims: claims,
////                expires: DateTime.Now.AddHours(2),
////                signingCredentials: credentials
////            );

////            return new JwtSecurityTokenHandler().WriteToken(token);
////        }

////    }
////}


//using Microsoft.AspNetCore.Mvc;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using WebApplication1.DataAccess;
//using WebApplication1.Models;

//namespace WebApplication1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class UsersController : ControllerBase
//    {
//        private readonly DAUser _daUser;
//        private readonly IConfiguration _configuration;

//        public UsersController(DAUser daUser, IConfiguration configuration)
//        {
//            _daUser = daUser;
//            _configuration = configuration;
//        }

//        [HttpPost("register")]
//        public async Task<IActionResult> Register([FromBody] registerModel request)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            if (await _daUser.EmailExists(request.Email))
//                return Conflict("Email already exists.");

//            int userId = await _daUser.RegisterUser(request);

//            return Ok(new
//            {
//                message = "User registered successfully.",
//                UserID = userId,
//                request.Username,
//                request.Email
//            });
//        }

//        [HttpPost("login")]
//        public async Task<IActionResult> Login([FromBody] LoginModel request)
//        {
//            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
//                return BadRequest("Email and password are required.");

//            var user = await _daUser.GetUserByEmail(request.Email);
//            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
//                return Unauthorized("Invalid email or password.");

//            try
//            {
//                var token = GenerateJwtToken(user);

//                return Ok(new
//                {
//                    message = "Login successful",
//                    token,
//                    user = new
//                    {
//                        user.UserID,
//                        user.Username,
//                        user.Email,
//                        user.Role
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("🔥 JWT generation error: " + ex.Message);
//                return StatusCode(500, "Internal Server Error: " + ex.Message);
//            }

//        }

//        [HttpPost("reset-password")]
//        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
//        {
//            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.NewPassword))
//                return BadRequest("Email and new password are required.");

//            bool updated = await _daUser.ResetPassword(model.Email, model.NewPassword);
//            if (!updated)
//                return NotFound("User not found.");

//            return Ok(new { message = "Password reset successfully." });
//        }

//        [HttpPut("UpdateRole/{id}")]
//        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string role)
//        {
//            if (string.IsNullOrEmpty(role))
//                return BadRequest("Role is required.");

//            bool updated = await _daUser.UpdateUserRole(id, role);
//            if (!updated)
//                return NotFound("User not found.");

//            return Ok(new { message = "User role updated successfully." });
//        }

//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteUser(int id)
//        {
//            bool deleted = await _daUser.DeleteUser(id);
//            if (!deleted)
//                return NotFound("User not found.");

//            return Ok("User deleted successfully.");
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAllUsers()
//        {
//            var users = await _daUser.GetAllUsers();
//            return Ok(users);
//        }

//        private string GenerateJwtToken(UserModel user)
//        {
//            var jwtKey = _configuration["JwtSettings:Key"];
//            var issuer = _configuration["JwtSettings:Issuer"];
//            var audience = _configuration["JwtSettings:Audience"];

//            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
//            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//            var claims = new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
//                new Claim(ClaimTypes.Name, user.Username),
//                new Claim(ClaimTypes.Email, user.Email),
//                new Claim(ClaimTypes.Role, user.Role ?? "User")
//            };

//            var token = new JwtSecurityToken(
//                issuer: issuer,
//                audience: audience,
//                claims: claims,
//                expires: DateTime.Now.AddHours(2),
//                signingCredentials: credentials
//            );

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DAUser _daUser;
        private readonly IConfiguration _configuration;

        public UsersController(DAUser daUser, IConfiguration configuration)
        {
            _daUser = daUser;
            _configuration = configuration;
        }

        // ✅ Register (Customer by default)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] registerModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _daUser.EmailExists(request.Email))
                return Conflict("Email already exists.");

            int userId = await _daUser.RegisterUser(request);

            return Ok(new
            {
                message = "User registered successfully.",
                UserID = userId,
                request.Username,
                request.Email
            });
        }

        // ✅ Login (Admin, Customer, Staff)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var user = await _daUser.GetUserByEmail(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            // Optional: restrict roles if needed
            var validRoles = new[] { "admin", "customer", "staff" };
            if (!validRoles.Contains(user.Role.ToLower()))
                return Forbid("Unauthorized role.");

            try
            {
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Login successful",
                    token,
                    user = new
                    {
                        user.UserID,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 JWT generation error: " + ex.Message);
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        // ✅ Reset Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.NewPassword))
                return BadRequest("Email and new password are required.");

            bool updated = await _daUser.ResetPassword(model.Email, model.NewPassword);
            if (!updated)
                return NotFound("User not found.");

            return Ok(new { message = "Password reset successfully." });
        }

        // ✅ Update User Role (e.g., customer → staff or admin → staff)
        [HttpPut("update-role/{id}")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string role)
        {
            var allowedRoles = new[] { "admin", "customer", "staff" };
            if (string.IsNullOrWhiteSpace(role) || !allowedRoles.Contains(role.ToLower()))
                return BadRequest("Invalid role.");

            bool updated = await _daUser.UpdateUserRole(id, role);
            if (!updated)
                return NotFound("User not found.");

            return Ok(new { message = "User role updated successfully." });
        }

        // ✅ Delete User
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            bool deleted = await _daUser.DeleteUser(id);
            if (!deleted)
                return NotFound("User not found.");

            return Ok("User deleted successfully.");
        }

        // ✅ Get All Users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _daUser.GetAllUsers();
            return Ok(users);
        }

        // 🔐 JWT Generator
        private string GenerateJwtToken(UserModel user)
        {
            var jwtKey = _configuration["JwtSettings:Key"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "user")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
