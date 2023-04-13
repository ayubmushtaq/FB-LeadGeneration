using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace LeadGen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public WebhooksController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult VerificationRequest( // Creating an Endpoint
            [FromQuery(Name = "hub.mode")] string mode = "", // This value will always be set to subscribe.
            [FromQuery(Name = "hub.challenge")] string challenge = "", // An int you must pass back to us.
            [FromQuery(Name = "hub.verify_token")] string verifyToken = "" // A string that that we grab from the Verify Token field in your app's App Dashboard. 
            )
        {
            if (verifyToken.Equals(_configuration.GetValue<string>("FB_app_settings:app_subscription_verification")))
            {
                int result = System.Int32.Parse(challenge); //An int you must pass back to us.
                return Ok
                (
                    result //challenge
                );

            }
            else
            {
                return StatusCode(400,
                    (new string[] {
                            "mode: " + mode,
                            "challenge: " + challenge,
                            "verify: " + verifyToken
                            }
                    ));
            }
        }

        // POST api/webhooks
        [HttpPost]
        public async Task<IActionResult> FaceBookAdsAsync([FromBody] Rootobject body)
        {
            string connectionString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            try
            {
                //var encodedUrl = Request.GetEncodedUrl();
                //string json = System.Text.Json.JsonSerializer.Serialize(body);

                //var entry = body.GetProperty("entry");
                //string entryString = System.Text.Json.JsonSerializer.Serialize(entry);

                //var changes = entry[0].GetProperty("changes");
                //string changesString = System.Text.Json.JsonSerializer.Serialize(changes);

                //var value = changes[0].GetProperty("value");
                //string valueString = System.Text.Json.JsonSerializer.Serialize(value);


                //string ad_idString = "";

                //if (value.ToString().Contains("ad_id") == true)
                //{
                //    var ad_id = value.GetProperty("ad_id");
                //    ad_idString = System.Text.Json.JsonSerializer.Serialize(ad_id);
                //    ad_idString = ad_id.ToString();
                //}

                //var form_id = value.GetProperty("form_id");

                //string form_idString = System.Text.Json.JsonSerializer.Serialize(form_id);

                //var leadgen_id = value.GetProperty("leadgen_id");
                //string leadgen_idString = System.Text.Json.JsonSerializer.Serialize(leadgen_id);

                //var page_id = value.GetProperty("page_id");
                //string page_idString = System.Text.Json.JsonSerializer.Serialize(page_id);

                //string adgroup_idString = "";

                //if (value.ToString().Contains("adgroup_id") == true)
                //{
                //    var adgroup_id = value.GetProperty("adgroup_id");
                //    adgroup_idString = System.Text.Json.JsonSerializer.Serialize(adgroup_id);
                //    adgroup_idString = adgroup_id.ToString();
                //}

                // Insert the lead data log into the SQL database
                // 

                var jsonObj = JsonConvert.SerializeObject(body);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO LeadLog (WebHooksJSON, AD_ID, Form_ID, LeadGen_ID, Page_ID, AD_Group_ID) VALUES (@WebHooksJSON, @AD_ID, @Form_ID, @LeadGen_ID, @Page_ID, @AD_Group_ID)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@WebHooksJSON", jsonObj);
                    command.Parameters.AddWithValue("@AD_ID", (body.entry[0].changes[0].value.ad_id == null ? "" : body.entry[0].changes[0].value.ad_id));
                    command.Parameters.AddWithValue("@Form_ID", body.entry[0].changes[0].value.form_id);
                    command.Parameters.AddWithValue("@LeadGen_ID", body.entry[0].changes[0].value.leadgen_id);
                    command.Parameters.AddWithValue("@Page_ID", body.entry[0].changes[0].value.page_id);
                    command.Parameters.AddWithValue("@AD_Group_ID", (body.entry[0].changes[0].value.adgroup_id == null ? "" : body.entry[0].changes[0].value.adgroup_id));
                    connection.Open();
                    int result = command.ExecuteNonQuery();
                    connection.Close();
                }
                string accessToken = _configuration.GetValue<string>("FB_app_settings:app_access_token");
                var leadUrl = $"https://graph.facebook.com/v16.0/{body.entry[0].changes[0].value.leadgen_id}?access_token={accessToken}";
                var formUrl = $"https://graph.facebook.com/v16.0/{body.entry[0].changes[0].value.form_id}?access_token={accessToken}";

                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage responsed = httpClient.GetAsync(leadUrl).Result;
                    if (responsed.IsSuccessStatusCode)
                    {
                        using (var httpClientLead = new HttpClient())
                        {
                            var response = await httpClientLead.GetStringAsync(formUrl);
                            if (!string.IsNullOrEmpty(response))
                            {
                                var jsonObjLead = JsonConvert.DeserializeObject<LeadFormData>(response);
                                //jsonObjLead.Name contains the lead ad name

                                //If response is valid get the field data
                                using (var httpClientFields = new HttpClient())
                                {
                                    var responseFields = await httpClientFields.GetStringAsync(leadUrl);
                                    if (!string.IsNullOrEmpty(responseFields))
                                    {
                                        var jsonObjFields = JsonConvert.DeserializeObject<LeadData>(responseFields);
                                        //jsonObjFields.FieldData contains the field value
                                        var fieldsCount = jsonObjFields.FieldData.Count();
                                        var nameFieldValue = "";
                                        var emailFieldValue = "";
                                        var phoneFieldValue = "";
                                        var otherFieldValue = "";
                                        foreach (var item in jsonObjFields.FieldData)
                                        {
                                            if (item.Name == "full_name")
                                            {
                                                nameFieldValue = item.Values.FirstOrDefault();
                                            }
                                            else if (item.Name == "email")
                                            {
                                                emailFieldValue = item.Values.FirstOrDefault();
                                            }
                                            else if (item.Name == "phone_number")
                                            {
                                                phoneFieldValue = item.Values.FirstOrDefault();
                                            }
                                            else
                                            {
                                                otherFieldValue += (otherFieldValue == "" ? "{" : ",") + string.Concat("'", item.Name, "':'", item.Values.FirstOrDefault(), "'");
                                            }
                                        }
                                        if (otherFieldValue != "")
                                        {
                                            otherFieldValue += "}";
                                        }

                                        // Insert the lead data into the SQL database

                                        using (SqlConnection connection = new SqlConnection(connectionString))
                                        {
                                            string query = "INSERT INTO Leads (LeadGenId, FormId, PageId, Name, Email, OtherColumn, CreatedOn) VALUES (@LeadGenId, @FormId, @PageId, @Name, @Email, @OtherColumn, @CreatedOn)";
                                            SqlCommand command = new SqlCommand(query, connection);
                                            command.Parameters.AddWithValue("@LeadGenId", body.entry[0].changes[0].value.leadgen_id);
                                            command.Parameters.AddWithValue("@FormId", body.entry[0].changes[0].value.form_id);
                                            command.Parameters.AddWithValue("@PageId", body.entry[0].changes[0].value.page_id);
                                            command.Parameters.AddWithValue("@Name", nameFieldValue);
                                            command.Parameters.AddWithValue("@Email", emailFieldValue);
                                            command.Parameters.AddWithValue("@Phone", phoneFieldValue);
                                            command.Parameters.AddWithValue("@Ad_Id", (body.entry[0].changes[0].value.ad_id == null ? "" : body.entry[0].changes[0].value.ad_id));
                                            command.Parameters.AddWithValue("@Ad_Group_Id", (body.entry[0].changes[0].value.adgroup_id == null ? "" : body.entry[0].changes[0].value.adgroup_id));
                                            command.Parameters.AddWithValue("@OtherColumn", otherFieldValue);
                                            command.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
                                            connection.Open();
                                            int result = command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                    }
                                }
                            }
                        }


                    }
                    else
                    {
                        // Handle the error response
                        string errorMessage = $"Failed to retrieve lead data from Facebook Graph API. Status code: {responsed.StatusCode}";
                        // Handle the error here, e.g. log the error message or send an email alert.
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return BadRequest(msg);
            }
        }
    }
}
public class LeadData
{
    [JsonProperty("created_time")]
    public string CreatedTime { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("field_data")]
    public List<FieldData> FieldData { get; set; }
}
public class FieldData
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("values")]
    public List<string> Values { get; set; }
}

public class LeadFormData
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}
public class Rootobject
{
    public string? _object { get; set; }
    public Entry[] entry { get; set; }
}

public class Entry
{
    public string id { get; set; }
    public int time { get; set; }
    public Change[] changes { get; set; }
}

public class Change
{
    public Value value { get; set; }
    public string field { get; set; }
}

public class Value
{
    public string form_id { get; set; }
    public string leadgen_id { get; set; }
    public int created_time { get; set; }
    public string page_id { get; set; }
    public string? ad_id { get; set; }
    public string? adgroup_id { get; set; }
}