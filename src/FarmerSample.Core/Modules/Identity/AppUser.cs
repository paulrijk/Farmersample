using Microsoft.AspNetCore.Identity;

namespace FarmerSample.Core.Modules.Identity;

public class AppUser : IdentityUser<Guid>
{
    public AppUser() => Id = Guid.CreateVersion7();
    public required string DisplayName { get; set; }
}
