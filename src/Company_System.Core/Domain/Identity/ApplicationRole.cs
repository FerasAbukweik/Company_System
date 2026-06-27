using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Domain.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public override Guid Id { get; set; } =  Guid.NewGuid();
}