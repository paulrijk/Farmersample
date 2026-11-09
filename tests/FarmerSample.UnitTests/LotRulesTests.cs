using FarmerSample.Core.Modules.SupplyQuality;

namespace FarmerSample.UnitTests;

public class CompositeShareTests
{
    [Fact]
    public void Shares_follow_volume() => Assert.Equal([30.0m, 70.0m], CompositeShares.VolumeWeighted([1800, 4200]));

    [Theory]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 333, 333, 334 })]
    [InlineData(new[] { 7, 13, 29, 51 })]
    [InlineData(new[] { 1, 999_999 })]
    public void Shares_always_add_up_to_exactly_100(int[] volumes) =>
        Assert.Equal(100m, CompositeShares.VolumeWeighted(volumes).Sum());

    [Fact]
    public void Single_farm_gets_100() => Assert.Equal([100m], CompositeShares.VolumeWeighted([900]));

    [Fact]
    public void Zero_volume_is_rejected() => Assert.Throws<ArgumentException>(() => CompositeShares.VolumeWeighted([100, 0]));
}

public class GradingVersionTests
{
    private static Grading G(int version, Guid? supersedes = null) => new() { Grade = "550 g/L", Operator = "qc", Version = version, SupersedesId = supersedes };

    [Fact]
    public void Current_grading_is_the_one_nothing_supersedes()
    {
        var v1 = G(1); var v2 = G(2, v1.Id); var v3 = G(3, v2.Id);
        var batch = new SampleBatch { Code = "SB-0000-0000", Product = "Black pepper", CollectedBy = "x", Gradings = [v3, v1, v2] };
        Assert.Same(v3, batch.CurrentGrading);
    }

    [Fact]
    public void Ungraded_batch_has_no_current_grading() =>
        Assert.Null(new SampleBatch { Code = "SB-0000-0000", Product = "Black pepper", CollectedBy = "x" }.CurrentGrading);
}
