namespace JwtIdentity.Models
{
    public class SurveyAnalysis : BaseModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Analysis { get; set; }
        public Survey Survey { get; set; }
    }
}
