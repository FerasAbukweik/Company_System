namespace HR_System.Core.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string InstanceName { get; set; } = "";
    // minutes
    public int SlidingExpiration { get; set; }
    // minutes
    public int AbsoluteExpiration { get; set; }
}
