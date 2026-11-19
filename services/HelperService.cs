using System.Security.Claims;
using System.Text;

public interface IHelperService
{
	string EncodeGroupName(string prefix, int id, string gn);
	int GetCurrentUserId();
	string GetCurrentUserRole();
}

public class HelperService: IHelperService
{
	private readonly IHttpContextAccessor _httpContextAccessor;

    public HelperService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

	public string EncodeGroupName(string prefix, int id, string gn)
    {
        var rgn = $"{prefix}-{id}-{gn}";

        return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(rgn))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
	public int GetCurrentUserId()
	{
		var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return int.TryParse(userId, out var id) ? id : 0;
	}
	public string GetCurrentUserRole()
	{
		return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
	}
}