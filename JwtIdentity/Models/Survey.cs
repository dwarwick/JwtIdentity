namespace JwtIdentity.Models
{
    public class Survey : BaseModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Guid { get; set; }
        public bool Complete { get; set; }
        public bool Published { get; set; }
        public List<Question> Questions { get; set; }
    }
}
