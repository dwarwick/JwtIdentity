namespace JwtIdentity.Common.ViewModels
{
    public class SurveyViewModel : BaseViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 5)]
        public string Title { get; set; }

        [Required]
        [StringLength(250, MinimumLength = 5)]
        public string Description { get; set; }

        public string Guid { get; set; }

        public List<QuestionViewModel> Questions { get; set; }
    }
}
