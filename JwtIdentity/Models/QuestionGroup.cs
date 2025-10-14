using System.ComponentModel.DataAnnotations.Schema;

namespace JwtIdentity.Models
{
    public class QuestionGroup : BaseModel
    {
        public int Id { get; set; }

        [ForeignKey("Survey")]
        public int SurveyId { get; set; }
        
        public int GroupNumber { get; set; }
        
        public string GroupName { get; set; }
        
        // Navigation to the next group, null means end of survey
        public int? NextGroupId { get; set; }
        
        // If true, submit survey after this group is complete
        public bool SubmitAfterGroup { get; set; }
        
        public Survey Survey { get; set; }
    }
}
