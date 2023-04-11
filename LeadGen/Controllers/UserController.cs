using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace LeadGen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("getUserPagesByUserId/{userId}")]
        public IActionResult getUserPagesByUserId(string userId)
        {
            DataTable dt = new();
            string connectionString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            using (SqlConnection connection = new(connectionString))
            {
                SqlDataAdapter command = new("usp_GetUserPages", connection);
                command.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                command.SelectCommand.Parameters.AddWithValue("@UserId", userId);
                command.Fill(dt);
            }
            return Ok(dt);
        }
        [HttpPost]
        [Route("addUsers/{userId}/{userName}/{accessToken}")]
        public async Task<IActionResult> addUsers(string userId,string userName, string accessToken)
        {
            string connectionString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new("usp_InsertUpdateUsers", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Username", userName);
                command.Parameters.AddWithValue("@AccessToken", accessToken);
                connection.Open();
                _ = command.ExecuteNonQuery();
                connection.Close();
            }
            return Ok();
        }
        [HttpPost]
        [Route("addUserPages/{userId}/{pageId}/{accessToken}")]
        public async Task<IActionResult> addUserPages(string userId, string pageId, string accessToken)
        {
            string connectionString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new("usp_InsertUpdateUserPages", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@PageId", pageId);
                command.Parameters.AddWithValue("@AccessToken", accessToken);
                connection.Open();
                _ = command.ExecuteNonQuery();
                connection.Close();
            }
            return Ok();
        }
    }
}
