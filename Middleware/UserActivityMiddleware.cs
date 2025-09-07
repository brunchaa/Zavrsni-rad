using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
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

        public async Task InvokeAsync(HttpContext context, UserManager<Korisnik> userManager, AppDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    user.LastActivityTime = DateTime.UtcNow; // Ažuriraj zadnju aktivnost
                    await userManager.UpdateAsync(user);
                    // Opcionalno: Dodaj u UserActivityLogs
                    // dbContext.UserActivityLogs.Add(new UserActivityLog { UserId = user.Id.ToString(), LoginTime = DateTime.UtcNow });
                    // await dbContext.SaveChangesAsync();
                }
            }

            await _next(context);
        }
    }
}