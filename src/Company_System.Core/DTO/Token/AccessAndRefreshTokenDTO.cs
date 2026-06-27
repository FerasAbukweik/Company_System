namespace HR_System.Core.DTO.Token;

public class AccessAndRefreshTokenDTO
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    
    
    
    // override
    public override string ToString()
    {
        return $"AccessToken: {AccessToken}, RefreshToken: {RefreshToken}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AccessAndRefreshTokenDTO other)
        {
            return false;
        }
        
        return (AccessToken == other.AccessToken && RefreshToken == other.RefreshToken);
    }
}