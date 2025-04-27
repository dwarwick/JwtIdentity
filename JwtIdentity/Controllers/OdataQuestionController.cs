using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace JwtIdentity.Controllers
{
    [Route("[controller]")]
    [Authorize] // Add this attribute to require authentication
    public class OdataQuestionController : ODataController
    {
        private readonly ApplicationDbContext _context;

        public OdataQuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        [HttpGet]
        public IQueryable<BaseQuestionDto> Get()
        {
            return _context.Questions.Select(q => new BaseQuestionDto
            {
                Id = q.Id,
                SurveyId = q.SurveyId,
                Text = q.Text,
                QuestionNumber = q.QuestionNumber,
                QuestionType = q.QuestionType,
                CreatedDate = q.CreatedDate,
                UpdatedDate = q.UpdatedDate
            });
        }
    }
}