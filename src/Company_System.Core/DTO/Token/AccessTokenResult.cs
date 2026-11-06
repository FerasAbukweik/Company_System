namespace HR_System.Core.DTO.Token;

public class AccessTokenResult
{
    public required string AccessToken { get; set; }
    public required DateTime Expires { get; set; }
}