using System.Text;

public interface IGroupNameEncoder
{
    string EncodeGroupName(string prefix, int id, string gn);
}
public class GroupNameEncoder: IGroupNameEncoder
{
    public string EncodeGroupName(string prefix, int id, string gn)
    {
        var rgn = $"{prefix}-{id}-{gn}";

        return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(rgn))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}