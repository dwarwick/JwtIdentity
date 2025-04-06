using AutoMapper;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FeedbackController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Feedback
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<FeedbackViewModel>>> GetFeedbacks()
        {
            var feedbacks = await _context.Feedbacks.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks));
        }

        // GET: api/Feedback/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FeedbackViewModel>> GetFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);

            if (feedback == null)
            {
                return NotFound();
            }

            return _mapper.Map<FeedbackViewModel>(feedback);
        }

        // POST: api/Feedback
        [HttpPost]
        [Authorize] // Changed from [AllowAnonymous] to [Authorize] to require authentication
        public async Task<ActionResult<FeedbackViewModel>> PostFeedback(FeedbackViewModel feedbackViewModel)
        {
            var feedback = _mapper.Map<Feedback>(feedbackViewModel);
            
            // Get authenticated user's ID from claims
            ///var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            //feedback.CreatedById = userId;

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            feedbackViewModel.Id = feedback.Id;
            return CreatedAtAction("GetFeedback", new { id = feedback.Id }, feedbackViewModel);
        }

        // PUT: api/Feedback/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutFeedback(int id, FeedbackViewModel feedbackViewModel)
        {
            if (id != feedbackViewModel.Id)
            {
                return BadRequest();
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _mapper.Map(feedbackViewModel, feedback);
            
            // Preserve the original CreatedById
            var originalCreatedById = feedback.CreatedById;
            _context.Entry(feedback).Property(f => f.CreatedById).CurrentValue = originalCreatedById;
            
            _context.Entry(feedback).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeedbackExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Feedback/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.Id == id);
        }
    }
}