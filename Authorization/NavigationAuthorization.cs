using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using System.Security.Claims;

namespace QwenHT.Authorization
{
    public class NavigationRequirement : IAuthorizationRequirement
    {
    }

    public class NavigationAuthorizationHandler : AuthorizationHandler<NavigationRequirement>
    {
        private readonly ApplicationDbContext _dbContext;

        public NavigationAuthorizationHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NavigationRequirement requirement)
        {
            if (!(context.Resource is HttpContext httpContext) || !context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            // Get user roles from claims
            var userRoles = context.User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value).ToList();

            if (!userRoles.Any())
            {
                context.Fail();
                return;
            }

            // Get the requested endpoint/route from the current request
            var requestPath = httpContext.Request.Path.Value?.ToLower() ?? "";

            // Determine which controller is being accessed based on the route
            // Example: "/api/staff" should check for staff navigation permissions
            string navigationRoute = "";
            if (requestPath.Contains("/api/staff"))
                navigationRoute = "manage/staff";
            else if (requestPath.Contains("/api/navigation"))
                navigationRoute = "manage/navigation";
            else if (requestPath.Contains("/api/roles"))
                navigationRoute = "manage/role"; // Assuming there's a role management navigation item
            else if (requestPath.Contains("/api/users"))
                navigationRoute = "manage/user";
            else if (requestPath.Contains("/api/optionvalues"))
                navigationRoute = "manage/option-value";
            else if (requestPath.Contains("/api/menus"))
                navigationRoute = "manage/menus";
            else if (requestPath.Contains("/api/dashboard"))
                navigationRoute = "app/dashboard";

            if (!string.IsNullOrEmpty(navigationRoute))
            {
                // Check if any of the user's roles have access to this navigation item
                var hasAccess = await _dbContext.RoleNavigations
                    .Join(_dbContext.NavigationItems,
                          rn => rn.NavigationItemId,
                          ni => ni.Id,
                          (rn, ni) => new { rn.RoleName, ni.Name, ni.Route })
                    .AnyAsync(joined => userRoles.Contains(joined.RoleName)
                                     && joined.Route.ToLower().Contains(navigationRoute.ToLower()));

                if (hasAccess)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // If we can't determine the specific navigation item or user doesn't have access
            context.Fail();
        }
    }
}