using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public class RecaptchaRequest
    {
        public string Token { get; set; }
    }

    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }

    // A model to deserialize the verification result.
    public class RecaptchaValidationResult
    {
        public bool Success { get; set; }
        public IEnumerable<string> ErrorCodes { get; set; }
    }
}
