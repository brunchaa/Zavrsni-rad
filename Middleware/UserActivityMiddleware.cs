using Microsoft.AspNetCore.Http;
using SkladisteRobe.Data;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkladisteRobe.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = await dbContext.Korisnici.FindAsync(userId);
                if (user != null)
                {
                    user.LastActivityTime = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
            }

            await _next(context);
        }
    }
}