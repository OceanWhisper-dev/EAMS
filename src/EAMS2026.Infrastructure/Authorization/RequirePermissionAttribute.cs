using Microsoft.AspNetCore.Authorization;

namespace EAMS2026.Infrastructure.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
        : base(permissionCode)
    {
    }
}