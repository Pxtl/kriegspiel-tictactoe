using PxtlCa.SystemCollectionsExtensions;

namespace MnkeyFog.Model.Tests;

public class SpaceTests {
    private PlayersState _playersState = new PlayersState(
        [new PlayerInfo("X"), new PlayerInfo("O")],
        RoundRobinPlayManager.Instance
    );
    private Indexed.PlayerIndexed PlayerX => _playersState.GetPlayerIndexed("X");
    private Indexed.PlayerIndexed PlayerO => _playersState.GetPlayerIndexed("O");

    [Fact]
    public void Constructor_Default_MarkCharIsNull() {
        var space = new Space();
        space.MarkIndex.Should().BeNull();
    }

    [Fact]
    public void Constructor_Default_KnownToPlayersSet_IsEmpty() {
        var space = new Space();
        space.KnownToPlayerIndicesSet.ToHashSet().Should().BeEmpty();
    }

    [Fact]
    public void ToString_NoPlayerShowsMarkChar() {
        var space = new Space();
        space.MarkIndex = PlayerX.Index;

        space.ToString(null, _playersState).Should().Be("X");
    }

    [Fact]
    public void ToString_KnownToPlayerShowsMarkChar() {
        var space = new Space();
        space.MarkIndex = PlayerX.Index;
        space.MakeKnownToPlayerIndex(PlayerO.Index);

        space.ToString(PlayerO, _playersState).Should().Be("X");
    }

    [Fact]
    public void ToString_UnknownToPlayerShowsSpace() {
        var space = new Space();
        space.MarkIndex = PlayerX.Index;

        space.ToString(PlayerO, _playersState).Should().Be(" ");
    }

    [Fact]
    public void MakeKnownToPlayer_AddsPlayerToSet() {
        var space = new Space();
        space.KnownToPlayerIndicesSet.ToHashSet().Should().BeEmpty();

        space.MakeKnownToPlayerIndex(PlayerO.Index);
        space.KnownToPlayerIndicesSet.ToHashSet().Should().Contain(PlayerO.Index);
    }

    [Fact]
    public void IsKnownToPlayer_ReturnsTrue_ForKnownPlayer() {
        var space = new Space();
        space.MarkIndex = PlayerX.Index;
        space.MakeKnownToPlayerIndex(PlayerO.Index);

        space.IsKnownToPlayerIndex(PlayerO.Index).Should().BeTrue();
    }

    [Fact]
    public void IsKnownToPlayer_ReturnsFalse_ForUnknownPlayer() {
        var space = new Space();
        space.MarkIndex = PlayerX.Index;

        space.IsKnownToPlayerIndex(PlayerO.Index).Should().BeFalse();
        space.IsKnownToPlayerIndex(PlayerX.Index).Should().BeFalse();
    }
}
