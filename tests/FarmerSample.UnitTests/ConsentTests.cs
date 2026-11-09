using FarmerSample.Core.Modules.SupplyQuality;

namespace FarmerSample.UnitTests;

public class ConsentTests
{
    private static FarmerConsent Consent(ConsentPurpose p, ConsentEvidence e = ConsentEvidence.SignedFormPhoto) => new()
    {
        Purposes = p, Evidence = e, Language = "si", ConsentTextVersion = "v1", ObtainedBy = "agent", ObtainedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Withdrawn_consent_is_not_active()
    {
        var c = Consent(ConsentPurpose.PublicProfile);
        c.WithdrawnAt = DateTimeOffset.UtcNow;
        Assert.False(c.IsActiveFor(ConsentPurpose.PublicProfile));
    }

    [Fact]
    public void Photos_need_a_signed_form_not_only_a_read_aloud_tick()
    {
        Assert.False(Consent(ConsentPurpose.Photos, ConsentEvidence.AgentReadAloud).IsActiveFor(ConsentPurpose.Photos));
        Assert.True(Consent(ConsentPurpose.Photos).IsActiveFor(ConsentPurpose.Photos));
    }

    [Fact]
    public void Consent_covers_only_the_purposes_given() =>
        Assert.False(Consent(ConsentPurpose.PublicProfile).IsActiveFor(ConsentPurpose.FamilyStory));
}
