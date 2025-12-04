//using System.Data.SqlClient;
//using WebApplication1.Models;
//namespace WebApplication1.DataAccess
//{
//    public class DAUser
//    {
//        private readonly string _connectionString;

//        public DAUser(IConfiguration config)
//        {
//            _connectionString = config.GetConnectionString("DefaultConnection");
//        }

//        public async Task<bool> EmailExists(string email)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@Email", email);
//            await connection.OpenAsync();
//            int count = (int)await command.ExecuteScalarAsync();
//            return count > 0;
//        }

//        public async Task<int> RegisterUser(registerModel request)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = @"
//                INSERT INTO Users (Username, Email, PasswordHash, Role)
//                OUTPUT INSERTED.UserID
//                VALUES (@Username, @Email, @PasswordHash, @Role)";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@Username", request.Username);
//            command.Parameters.AddWithValue("@Email", request.Email);
//            command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(request.Password));
//            command.Parameters.AddWithValue("@Role", "customer");

//            await connection.OpenAsync();
//            return (int)await command.ExecuteScalarAsync();
//        }

//        public async Task<UserModel?> GetUserByEmail(string email)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = "SELECT * FROM Users WHERE Email = @Email";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@Email", email);

//            await connection.OpenAsync();
//            using var reader = await command.ExecuteReaderAsync();
//            if (await reader.ReadAsync())
//            {
//                return new UserModel
//                {
//                    UserID = (int)reader["UserID"],
//                    Username = reader["Username"].ToString(),
//                    Email = reader["Email"].ToString(),
//                    PasswordHash = reader["PasswordHash"].ToString(),
//                    Role = reader["Role"].ToString()
//                };
//            }

//            return null;
//        }

//        public async Task<bool> ResetPassword(string email, string newPassword)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(newPassword));
//            command.Parameters.AddWithValue("@Email", email);

//            await connection.OpenAsync();
//            return await command.ExecuteNonQueryAsync() > 0;
//        }

//        public async Task<bool> UpdateUserRole(int userId, string role)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = "UPDATE Users SET Role = @Role WHERE UserID = @UserID";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@Role", role);
//            command.Parameters.AddWithValue("@UserID", userId);

//            await connection.OpenAsync();
//            return await command.ExecuteNonQueryAsync() > 0;
//        }

//        public async Task<bool> DeleteUser(int userId)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var query = "DELETE FROM Users WHERE UserID = @UserID";
//            using var command = new SqlCommand(query, connection);
//            command.Parameters.AddWithValue("@UserID", userId);

//            await connection.OpenAsync();
//            return await command.ExecuteNonQueryAsync() > 0;
//        }

//        public async Task<List<object>> GetAllUsers()
//        {
//            var users = new List<object>();
//            using var connection = new SqlConnection(_connectionString);
//            var query = "SELECT UserID, Username, Email, Role FROM Users";

//            using var command = new SqlCommand(query, connection);
//            await connection.OpenAsync();
//            using var reader = await command.ExecuteReaderAsync();
//            while (await reader.ReadAsync())
//            {
//                users.Add(new
//                {
//                    UserID = (int)reader["UserID"],
//                    Username = reader["Username"].ToString(),
//                    Email = reader["Email"].ToString(),
//                    Role = reader["Role"].ToString()
//                });
//            }

//            return users;
//        }
//    }
//}

using System.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.DataAccess
{
    public class DAUser
    {
        private readonly string _connectionString;

        public DAUser(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<bool> EmailExists(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);
            await connection.OpenAsync();
            int count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<int> RegisterUser(registerModel request)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"
                INSERT INTO Users (Username, Email, PasswordHash, Role)
                OUTPUT INSERTED.UserID
                VALUES (@Username, @Email, @PasswordHash, @Role)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", request.Username);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(request.Password));

            // Default role is "customer" on registration — admin can later change to "staff" or "admin"
            command.Parameters.AddWithValue("@Role", "customer");

            await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<UserModel?> GetUserByEmail(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Users WHERE Email = @Email";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserModel
                {
                    UserID = (int)reader["UserID"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    PasswordHash = reader["PasswordHash"].ToString(),
                    Role = reader["Role"].ToString() // Could be "admin", "customer", or "staff"
                };
            }

            return null;
        }

        public async Task<bool> ResetPassword(string email, string newPassword)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(newPassword));
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateUserRole(int userId, string role)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE Users SET Role = @Role WHERE UserID = @UserID";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Role", role); // "admin", "customer", "staff"
            command.Parameters.AddWithValue("@UserID", userId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteUser(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "DELETE FROM Users WHERE UserID = @UserID";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserID", userId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<object>> GetAllUsers()
        {
            var users = new List<object>();
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT UserID, Username, Email, Role FROM Users";

            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new
                {
                    UserID = (int)reader["UserID"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    Role = reader["Role"].ToString()
                });
            }

            return users;
        }
    }
}
