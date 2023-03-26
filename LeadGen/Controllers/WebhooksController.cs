using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeadGen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly string SUBSCRIPTION_VERIFICATION = "leadAdsAyubTest";
        [HttpGet]
        public IActionResult VerificationRequest( // Creating an Endpoint
            [FromQuery(Name = "hub.mode")] string mode = "", // This value will always be set to subscribe.
            [FromQuery(Name = "hub.challenge")] string challenge = "", // An int you must pass back to us.
            [FromQuery(Name = "hub.verify_token")] string verifyToken = "" // A string that that we grab from the Verify Token field in your app's App Dashboard. 
            )
        {
            if (verifyToken.Equals(SUBSCRIPTION_VERIFICATION))
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
        public async Task<IActionResult> FaceBookAdsAsync([FromBody] JsonElement body)
        {
            try
            {
                var encodedUrl = Request.GetEncodedUrl();
                string json = System.Text.Json.JsonSerializer.Serialize(body);

                var entry = body.GetProperty("entry");
                string entryString = System.Text.Json.JsonSerializer.Serialize(entry);

                var changes = entry[0].GetProperty("changes");
                string changesString = System.Text.Json.JsonSerializer.Serialize(changes);

                var value = changes[0].GetProperty("value");
                string valueString = System.Text.Json.JsonSerializer.Serialize(value);

                //var ad_id = value.GetProperty("ad_id");
                //string ad_idString = System.Text.Json.JsonSerializer.Serialize(ad_id);

                var form_id = value.GetProperty("form_id");
                
                string form_idString = System.Text.Json.JsonSerializer.Serialize(form_id);

                var leadgen_id = value.GetProperty("leadgen_id");
                string leadgen_idString = System.Text.Json.JsonSerializer.Serialize(leadgen_id);

                var page_id = value.GetProperty("page_id");
                string page_idString = System.Text.Json.JsonSerializer.Serialize(page_id);

                //var adgroup_id = value.GetProperty("adgroup_id");
                //string adgroup_idString = System.Text.Json.JsonSerializer.Serialize(adgroup_id);

                string accessToken = "EAACHik0OD1sBALTU6dqQT76BZBcmxFf5YEz0O3eeXGZCtBdcrdZBsSQuLrGNsvhihb49ZAycyfMH3yb84843L29rTvXZAguhiH6l38ZB89KaZAwF3mMZBsAbZAfYDMfmQGZArILx79ZBj9HoZAmMfak2CT50vyBiTLpBAP5GFIQTQjsoY9HtAogM6oAZADjiWHNdZCWYZArqLelqzi0JmZBJfHmkxwEa";
                var leadUrl = $"https://graph.facebook.com/v16.0/{leadgen_id}?access_token={accessToken}";
                var formUrl = $"https://graph.facebook.com/v16.0/{form_id}?access_token={accessToken}";

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
                                            else
                                            {
                                                otherFieldValue += (otherFieldValue == "" ? "{" : ",") + string.Concat("'", item.Name, "':'", item.Values.FirstOrDefault(), "'");
                                            }
                                        }
                                        otherFieldValue += "}";
                                        // Insert the lead data into the SQL database
                                        string connectionString = "Server = tcp:externaldb.database.windows.net,1433;Initial Catalog=external; Persist Security Info=False; User ID=extuser; Password=TestStagDB123!; MultipleActiveResultSets=False; Encrypt=True; TrustServerCertificate=False; Connection Timeout=30;";
                                        using (SqlConnection connection = new SqlConnection(connectionString))
                                        {
                                            string query = "INSERT INTO Leads (LeadGenId, FormId, PageId, Name, Email, OtherColumn, CreatedOn) VALUES (@LeadGenId, @FormId, @PageId, @Name, @Email, @OtherColumn, @CreatedOn)";
                                            SqlCommand command = new SqlCommand(query, connection);
                                            command.Parameters.AddWithValue("@LeadGenId", leadgen_id.ToString());
                                            command.Parameters.AddWithValue("@FormId", form_id.ToString());
                                            command.Parameters.AddWithValue("@PageId", page_id.ToString());
                                            command.Parameters.AddWithValue("@Name", nameFieldValue);
                                            command.Parameters.AddWithValue("@Email", emailFieldValue);
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

                return Ok
                (
                    new string[] {
                    "POST webhooks",
                    "public IActionResult YouGotANewLead([FromBody] string content)",
                    "encoded url: " + encodedUrl,
                    "this is what we received in the body: " + json,
                    "entry: " + entryString,
                    "changes: " + changesString,
                    "value: " + valueString,
                    "leadgen_id: " + leadgen_idString
                    }
                );
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

    [JsonProperty("leadgen_export_csv_url")]
    public string CsvExportUrl { get; set; }

    [JsonProperty("locale")]
    public string Locale { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}