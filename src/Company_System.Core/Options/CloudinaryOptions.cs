namespace HR_System.Core.Options;

public class CloudinaryOptions
{
    public const string SectionName = "CloudinarySettings";

    public string CloudName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
}
