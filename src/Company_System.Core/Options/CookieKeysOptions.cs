namespace HR_System.Core.Options;

public class CookieKeysOptions
{
    public const string SectionName = "CookieKeys";

    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}
