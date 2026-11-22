using System.Security.Claims;
using biddingServer.dto.Hubs;

namespace biddingServer.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ValidateSignalRTokenAttribute : Attribute
{
    public HubRoles Roles { get; set; } = HubRoles.None;

    public bool RoleCheck(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;
        if (HubRoles.None == Roles) return true;

        var raw = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
        var ok = Enum.TryParse<HubRoles>(raw, true, out var role);

        return ok && (Roles & role) != 0;
    }
}