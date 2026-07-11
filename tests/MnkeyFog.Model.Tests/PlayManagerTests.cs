
namespace MnkeyFog.Model.Tests;

public class PlayManagerTests {
    
    #region unique player marks
    [Fact]
    public void RoundRobinPlayManagerConstructor_WithUniqueMarksIsAllowed() {
        var expectedPlayerInfos = new List<PlayerInfo>() { new("X"), new("O") };
        var actualManager = new PlayersState(expectedPlayerInfos, RoundRobinPlayManager.Instance);
        actualManager.PlayerInfos.Should().BeEquivalentTo(expectedPlayerInfos);
    }

    [Fact]
    public void RoundRobinPlayManagerConstructor_WithNonUniqueMarksThrows() {
        var expectedPlayerInfos = new List<PlayerInfo>() { new("X"), new("O"), new("X") };
        var action = () => {
            _ = new PlayersState(expectedPlayerInfos, RoundRobinPlayManager.Instance);
        };
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundRobinPlayManagerConstructor_WithMarksSameButDifferentCaseThrows() {
        var expectedPlayerInfos = new List<PlayerInfo>() { new("X"), new("O"), new("x") };
        var action = () => {
            _ = new PlayersState(expectedPlayerInfos, RoundRobinPlayManager.Instance);
        };
        action.Should().Throw<ArgumentException>();
    }
    #endregion

    [Fact]
    public void GameStateConstructor_WithBoardsCreatesProperState() {
        var playerXIndex = 0;
        var playerOIndex = 1;
        var state = new GameState(
            [new PlayerInfo("X"), new PlayerInfo("O")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3), MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );
        state.Boards.Count.Should().Be(2);
        state.PlayersState.PlayerInfos.Should().Contain(new PlayerInfo("X"));
        state.PlayersState.PlayerInfos.Should().Contain(new PlayerInfo("O"));
        state.PlayersState.ActivePlayerIndices.Should().Contain(playerXIndex);
        state.PlayersState.ActivePlayerIndices.Should().Contain(playerOIndex);
    }

    [Fact]
    public void Round_RoundIndexStartsAtZero() {
        var playerXIndex = 0;
        var state = new GameState(
            [new PlayerInfo("X"), new PlayerInfo("O")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );
        state.PlayersState.RoundIndex.Should().Be(playerXIndex);
    }

    [Fact]
    public void EndTurn_AdvancesTurn() {
        var playerXIndex = 0;
        var playerOIndex = 1;
        var state = new GameState(
            [new PlayerInfo("X"), new PlayerInfo("O")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.EndTurn(playerXIndex, out _);
        state.PlayersState.ActivePlayerIndices.Should().Contain(playerOIndex);
    }

    [Fact]
    public void EndTurn_EndRound_TracksRoundIndex() {
        var playerXIndex = 0;
        var playerOIndex = 1;
        var state = new GameState(
            [new PlayerInfo("X"), new PlayerInfo("O")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.EndTurn(playerXIndex, out _);
        state.EndTurn(playerOIndex, out _);
        state.PlayersState.IsRoundOver.Should().BeTrue();
        state.PlayersState.GameStateText.Should().Be("Round over.");

        state.EndRound(out _);
        state.PlayersState.RoundIndex.Should().Be(1);
    }

    [Fact]
    public void RoundComplete_OnePlayerResigned() {
        var playerXIndex = 0;
        var playerOIndex = 1;
        var state = new GameState(
            [new PlayerInfo("X"), new PlayerInfo("O")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerXIndex);
        state.EndTurn(playerOIndex, out _);

        state.PlayersState.ActivePlayers.Count().Should().Be(1);
        state.PlayersState.IsRoundOver.Should().BeTrue();
        state.PlayersState.GameStateText.Should().Be("Round over.");
    }

    [Fact]
    public void RoundComplete_TwoPlayers() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: true, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.EndTurn(playerAIndex, out _);
        state.EndTurn(playerBIndex, out _);
        state.PlayersState.IsRoundOver.Should().BeTrue();
        state.PlayersState.GameStateText.Should().Be("Synchronized play. Round complete.");
    }

    [Fact]
    public void RoundComplete_ThreePlayers() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var playerCIndex = 2;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: true, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.NumberOfActivePlayers.Should().Be(3);

        state.EndTurn(playerAIndex, out _);
        state.EndTurn(playerBIndex, out _);
        state.EndTurn(playerCIndex, out _);
        state.PlayersState.IsRoundOver.Should().BeTrue();
        state.PlayersState.GameStateText.Should().Be("Synchronized play. Round complete.");
    }

    [Fact]
    public void ResignPlayerInRoundRobinMode_OnlyNextPlayerCanTakeTurn() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var playerCIndex = 2;
        var playerDIndex = 3;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerAIndex);

        state.PlayersState.CanTakeTurn(playerAIndex).Should().BeFalse();
        state.PlayersState.CanTakeTurn(playerBIndex).Should().BeTrue();
        state.PlayersState.CanTakeTurn(playerCIndex).Should().BeFalse();
        state.PlayersState.CanTakeTurn(playerDIndex).Should().BeFalse();
    }

    [Fact]
    public void ActivePlayers_ExcludesResignedPlayers() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var playerCIndex = 2;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerAIndex);
        state.PlayersState.ActivePlayerIndices.Should().Contain(playerBIndex);
        state.PlayersState.ActivePlayerIndices.Should().Contain(playerCIndex);
    }

    [Fact]
    public void ResignPlayer_AddsToResignedPlayersSet() {
        var playerAIndex = 0;

        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerAIndex);
        state.PlayersState.ResignedPlayerIndicesSet.Should().Contain(playerAIndex);
    }

    [Fact]
    public void ResignPlayer_SkipsResignedTurn() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerAIndex);
        state.PlayersState.ActivePlayerIndices.First().Should().Be(playerBIndex);
    }

    [Fact]
    public void GameStateConstructor_3Players_FirstPlayerIs_A() {
        var playerAIndexed = new PlayerIndexed("A", 0);

        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ActivePlayers.First().Should().Be(playerAIndexed);
    }

    [Fact]
    public void GameStateConstructor_RandomPlayer() {
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: false, isKriegspiel: true),
            isRandomPlayerOrder: true
        );
        var firstPlayer = state.PlayersState.ActivePlayers.Select(ap => ap.Index).First();
        var expectedPlayerInfos = new[] {0,1,2};
        expectedPlayerInfos.Contains(firstPlayer).Should().BeTrue();
    }

    [Fact]
    public void CanTakeTurn_AllAvailablePlayersSynchronousMode() {
        var playerAIndex = 0;
        var playerBIndex = 1;
        var playerCIndex = 2;
        var playerDIndex = 3;
        var state = new GameState(
            [new PlayerInfo("A"), new PlayerInfo("B"), new PlayerInfo("C")],
            new MNKTemplate([MNKBoardRuleset.CreateBoardBuilder(3, 3)], isSynchronousMode: true, isKriegspiel: true),
            isRandomPlayerOrder: false
        );

        state.PlayersState.ResignPlayer(playerAIndex);
        
        state.PlayersState.CanTakeTurn(playerAIndex).Should().BeFalse();
        state.PlayersState.CanTakeTurn(playerBIndex).Should().BeTrue();
        state.PlayersState.CanTakeTurn(playerCIndex).Should().BeTrue();
        
        // player D does not exist.
        state.PlayersState.CanTakeTurn(playerDIndex).Should().BeFalse();
    }

}
