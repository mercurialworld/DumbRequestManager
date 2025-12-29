namespace DumbRequestManager.Classes;

public class MapRequest
{
    public string Key { get; set; } = null!;
    public string? User { get; set; }
    public bool IsWip { get; set; }

    public MapRequest(string key, string? user = null, bool isWip = false)
    {
        Key = key;
        User = user;
        IsWip = isWip;
    }

    public MapRequest() { }
}