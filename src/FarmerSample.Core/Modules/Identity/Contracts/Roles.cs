namespace FarmerSample.Core.Modules.Identity.Contracts;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string QualityController = "QualityController";
    public const string FieldAgent = "FieldAgent";
    public const string Buyer = "Buyer";

    public static readonly string[] All = [Admin, Staff, QualityController, FieldAgent, Buyer];
    public static readonly string[] StaffRoles = [Admin, Staff, QualityController, FieldAgent];
}

public sealed record CurrentUserDto(Guid Id, string Email, string DisplayName, string[] Roles, bool MfaEnabled, bool MfaSatisfied);
