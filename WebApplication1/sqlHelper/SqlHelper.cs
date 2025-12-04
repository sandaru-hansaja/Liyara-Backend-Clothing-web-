using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
namespace WebApplication1.sqlHelper
{
    public class SqlHelper
    {
        public static async Task<List<T>> ExecuteReaderAsync<T>(
            SqlConnection conn,
            string query,
            Func<SqlDataReader, T> mapFunc,
            Dictionary<string, object>? parameters = null,

            SqlTransaction? transaction = null)
        {
            var results = new List<T>();
            using var cmd = new SqlCommand(query, conn, transaction);

            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(mapFunc(reader));
            }
            return results;
        }

        public static async Task<object?> ExecuteScalarAsync(
            SqlConnection conn,
            string query,
            Dictionary<string, object>? parameters = null,
            SqlTransaction? transaction = null)
        {
            using var cmd = new SqlCommand(query, conn, transaction);
            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
            return await cmd.ExecuteScalarAsync();
        }

        public static async Task<int> ExecuteNonQueryAsync(
            SqlConnection conn,
            string query,
            Dictionary<string, object>? parameters = null,
            SqlTransaction? transaction = null)
        {
            using var cmd = new SqlCommand(query, conn, transaction);
            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
