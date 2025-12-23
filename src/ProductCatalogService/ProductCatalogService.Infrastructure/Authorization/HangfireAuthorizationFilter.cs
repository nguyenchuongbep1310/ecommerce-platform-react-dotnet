using Hangfire.Dashboard;

namespace ProductCatalogService.Infrastructure.Authorization;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In production, you should implement proper authentication
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development, allow all access
        // In production, implement proper authentication:
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.Identity?.IsAuthenticated ?? false;
        
        return true; // Allow all for now
    }
}
