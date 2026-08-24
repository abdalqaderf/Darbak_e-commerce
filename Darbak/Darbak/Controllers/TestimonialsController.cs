using Darbak.Data;
using Darbak.Models;
using Darbak.Models.Enums;
using Darbak.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Darbak.Controllers
{
    public class TestimonialsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestimonialsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Public testimonial browsing now lives in the Home page carousel.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(
                "Index",
                "Home",
                fragment: "customer-feedback");
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new TestimonialCreateViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestimonialCreateViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            if (!string.IsNullOrWhiteSpace(model.Content))
            {
                model.Content = model.Content.Trim();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var testimonial = new Testimonial
            {
                Content = model.Content,
                Rating = model.Rating,
                UserId = userId,
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();

            TempData["TestimonialSuccess"] =
                "Your feedback was submitted and is waiting for approval.";

            return RedirectToAction(
                "Index",
                "Home",
                fragment: "customer-feedback");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminIndex()
        {
            var testimonials =
                await _context.Testimonials
                    .AsNoTracking()
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

            return View(testimonials);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
            {
                return NotFound();
            }

            testimonial.Status = ApprovalStatus.Approved;
            await _context.SaveChangesAsync();

            TempData["TestimonialSuccess"] =
                "Testimonial approved successfully.";

            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
            {
                return NotFound();
            }

            testimonial.Status = ApprovalStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["TestimonialSuccess"] =
                "Testimonial rejected successfully.";

            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
