using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        public IActionResult FaceBookAds([FromBody] JsonElement body)
        {
            var encodedUrl = Request.GetEncodedUrl();
            string json = System.Text.Json.JsonSerializer.Serialize(body);

            /* just a sample below...
            {
                "object":"page",
                "entry":[
                    {
                        "id":"987987987",
                        "time":1630087927,
                        "changes":[
                            {
                            "value":{
                                "form_id":"456456456",
                                "leadgen_id":"321321321", <=======
                                "created_time":1630087926,
                                "page_id":"123123123"
                            },
                            "field":"leadgen"
                            }
                        ]
                    }
                ]
            }            
            */

            var entry = body.GetProperty("entry");
            string entryString = System.Text.Json.JsonSerializer.Serialize(entry);

            var changes = entry[0].GetProperty("changes");
            string changesString = System.Text.Json.JsonSerializer.Serialize(changes);

            var value = changes[0].GetProperty("value");
            string valueString = System.Text.Json.JsonSerializer.Serialize(value);

            var leadgen_id = value.GetProperty("leadgen_id");
            string leadgen_idString = System.Text.Json.JsonSerializer.Serialize(leadgen_id);

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
    }
}
