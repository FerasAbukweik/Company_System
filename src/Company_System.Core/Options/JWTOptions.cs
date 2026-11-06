namespace HR_System.Core.Options;

public class JWTOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    // minutes. must be properties (not fields) or the configuration binder skips them
    public int AccessTokenLifeTime { get; set; }
    public int RefreshTokenLifeTime { get; set; }
    public string Key { get; set; } = "";
}
