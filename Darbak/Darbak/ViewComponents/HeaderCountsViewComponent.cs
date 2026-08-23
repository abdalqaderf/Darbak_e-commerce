using System.Security.Claims;
using System.Text.Json;
using Darbak.Data;
using Darbak.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darbak.ViewComponents
{
    public class HeaderCountsViewComponent : ViewComponent
    {
        private const string CartSessionKey = "Cart";
        private readonly ApplicationDbContext _context;

        public HeaderCountsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new HeaderCountsViewModel();

            var httpContext = ViewContext.HttpContext;

            var cartJson = httpContext.Session.GetString(CartSessionKey);
            if (!string.IsNullOrWhiteSpace(cartJson))
            {
                try
                {
                    var cart = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
                    model.CartCount = cart?.Sum(item => item.Quantity) ?? 0;
                }
                catch (JsonException)
                {
                    model.CartCount = 0;
                }
            }

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    model.WishlistCount = await _context.WishlistItems
                        .AsNoTracking()
                        .CountAsync(item => item.UserId == userId);
                }
            }

            return View(model);
        }
    }
}
