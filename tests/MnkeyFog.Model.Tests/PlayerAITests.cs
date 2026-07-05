using MnkeyFog.Model.Indexed;
using MnkeyFog.Model.PlayerAIs;

namespace MnkeyFog.Model.Tests;

public class PlayerAITests {
    [Fact]
    public void AIGameRunner_BasicTicTacToeGameEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new RandomAI(),
            [new Player("O")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.BasicTicTacToe, playerAIs);
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void AIGameRunner_KriegspielTicTacToeGameEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new RandomAI(),
            [new Player("O")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.KriegspielTicTacToe, playerAIs);
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void AIGameRunner_Match3GameEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new RandomAI(),
            [new Player("O")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.Match3, playerAIs);
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void AIGameRunner_FreestyleGomokuEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new RandomAI(),
            [new Player("O")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.FreestyleGomoku, playerAIs);
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void AIGameRunner_FogGomokuEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("A")] = new RandomAI(),
            [new Player("B")] = new RandomAI(),
            [new Player("C")] = new RandomAI(),
            [new Player("D")] = new RandomAI(),
            [new Player("E")] = new RandomAI(),
            [new Player("F")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.FogGomoku, playerAIs);
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void AIGameRunner_FogTicTacToeEnds() {
        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("A")] = new RandomAI(),
            [new Player("B")] = new RandomAI(),
            [new Player("C")] = new RandomAI(),
            [new Player("D")] = new RandomAI()
        };
        var action = () => {
            AIGameRunner.RunAIGame(GameTemplates.FogTicTacToe, playerAIs);
        };
        action.Should().NotThrow();
    }

    //commented out because Clod cannot consistently defeat Randy
    [Fact]
    public void AIGameRunner_BasicTicTacToe_AsterAIvsRandom() {
        // AsterAI vs RandomAI should show AsterAI winning more often
        int iterations = 10;

        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new AsterAI(),
            [new Player("O")] = new RandomAI()
        };
        var asterAIPlayerXIndex = 0;
        var scoreSum = ScoreCard.Empty;
        for (int i = 0; i < iterations; i++) {
            scoreSum += AIGameRunner.RunAIGame(GameTemplates.BasicTicTacToe, playerAIs, out var gameState);
            Console.Out.WriteLine(BoardRenderer.DrawBoards(gameState.GetSpectatorView(), 100));
            Console.Out.WriteLine(gameState.GameStateText);
        }
        scoreSum.Highest.PlayerScores.Count().Should().Be(1);
        scoreSum.Highest.PlayerScores.Single().PlayerIndex.Should().Be(asterAIPlayerXIndex);
    }

    //commented out because Clod cannot consistently defeat Randy
    [Fact]
    public void AIGameRunner_BasicTicTacToe_MontyAIvsAsterAI() {
        // AsterAI vs RandomAI should show AsterAI winning more often
        int iterations = 100;

        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new MontyAI(),
            [new Player("O")] = new AsterAI()
        };
        var montyAIPlayerXIndex = 0;
        var scoreSum = ScoreCard.Empty;
        for (int i = 0; i < iterations; i++) {
            scoreSum += AIGameRunner.RunAIGame(GameTemplates.BasicTicTacToe, playerAIs, out var gameState);
            Console.Out.WriteLine(BoardRenderer.DrawBoards(gameState.GetSpectatorView(), 100));
            Console.Out.WriteLine(gameState.GameStateText);
        }
        scoreSum.Highest.PlayerScores.Count().Should().Be(1);
        scoreSum.Highest.PlayerScores.Single().PlayerIndex.Should().Be(montyAIPlayerXIndex);
    }

    [Fact]
    public void MontyAI_KnowsToBlock() {
        var playerX = new PlayerIndexed("X", 0);
        var playerO = new PlayerIndexed("O", 1);
        var gameState = new GameState([playerX.Player, playerO.Player], GameTemplates.BasicTicTacToe, isRandomPlayerOrder:false);
        gameState.Boards[0].Spaces[1,1].MarkIndex = playerO.Index;
        gameState.Boards[0].Spaces[2,0].MarkIndex = playerO.Index;
        gameState.Boards[0].Spaces[2,2].MarkIndex = playerX.Index;
        gameState.Boards[0].Spaces[2,1].MarkIndex = playerX.Index;
        var gameView = gameState.GetView(playerX);
        var montyAI = new MontyAI();
        var gameAction = montyAI.FindOptimalGameAction(gameView)!;
        gameAction.GetType().Should().Be(typeof(MNKAction));
        var mnkAction = (MNKAction)gameAction;
        mnkAction.BoardIndex.Should().Be(0);
        mnkAction.Col.Should().Be(0);
        mnkAction.Row.Should().Be(2);
    }

    [Fact]
    public void AIGameRunner_FogTicTacToe_AsterAIvsRandom() {
        // AsterAI vs RandomAI should show AsterAI winning more often
        int iterations = 10;

        var playerAIs = new OrderedDictionary<Player, IPlayerAI> {
            [new Player("X")] = new AsterAI(),
            [new Player("O")] = new RandomAI()
        };
        var playerState = new PlayersState(playerAIs.Keys, RoundRobinPlayManager.Instance);
        var asterAIPlayerX = playerAIs.Keys.First();
        var scoreSum = ScoreCard.Empty;
        for (int i = 0; i < iterations; i++) {
            var result = AIGameRunner.RunAIGame(GameTemplates.FogTicTacToe, playerAIs, out var gameState);
            // add 1 point per-win.
            scoreSum += new ScoreCard(
                result.Highest.PlayerScores.Select(ps => new PlayerIndexScore(ps.PlayerIndex, 1))
            );
            Console.Out.WriteLine(BoardRenderer.DrawBoards(gameState.GetSpectatorView(), 100));
            Console.Out.WriteLine(gameState.GameStateText);
        }
        scoreSum.Highest.AsPlayers(playerState).Count().Should().Be(1);
        scoreSum.Highest.AsPlayers(playerState).Single().Should().Be(asterAIPlayerX);
    }
}
