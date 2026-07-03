using MnkeyFog.Model.Template;
using MnkeyFog.Model.Views;

namespace MnkeyFog.Model.PlayerAIs;

[ModelSerializable]
public abstract class MonteCarloAI : IPlayerAI {
    
    //currently depth > 4 has dire performance.
    public abstract string Description { get; }
    public abstract int MaxDepth { get; }
    Random _random = new Random();

    public void Attempt(GameView gameView) {
        var optimalGameAction = FindOptimalGameAction(gameView);
        if(optimalGameAction != null) {
            gameView.Attempt(optimalGameAction);
        }
    }

    public GameAction? FindOptimalGameAction(GameView gameView) {
        var simulatedState = CloneGameState(gameView);
        var depth = 0;
        var optimalActionAssessment = SimulateTurn(simulatedState, gameView.Player!, gameView.Player!, depth);
        return optimalActionAssessment?.GameAction;
    }

	private ActionAssessment? SimulateTurn(
        GameState simulatedState,
        Player currentPlayer,
        Player objectivePlayer,
        int depth
    ) {
        var actionFactories = simulatedState.GameTemplate.GetAvailableActions(simulatedState, currentPlayer);
        var actionAssessments = new List<ActionAssessment>();
        
        foreach (var factory in actionFactories) {
            GameAction? action = null;
            if (factory is GameActionFactoryForSimple factoryForSimple) {
                action = factoryForSimple.Create();
                actionAssessments.Add(
                    SimulateAction(CloneGameState(simulatedState), action, currentPlayer, objectivePlayer)
                );
            }
            if (factory is GameActionFactoryForSpace factoryForSpace) {
                for(sbyte boardIndex = 0; boardIndex < simulatedState.Boards.Count; boardIndex += 1) {
                    var board = simulatedState.GetBoardByIndex(boardIndex);
                    foreach(var spaceEnumerator in board.AsSpaceEnumerable()) {
                        if (spaceEnumerator.Space.Mark == null) {
                            action = factoryForSpace.Create(boardIndex, spaceEnumerator.Col, spaceEnumerator.Row);
                            actionAssessments.Add(
                                SimulateAction(CloneGameState(simulatedState), action, currentPlayer, objectivePlayer)
                            );
                        }
                    }
                }
            }
            if (factory is GameActionFactoryForBoard factoryForBoard) {
                for(sbyte boardIndex = 0; boardIndex < simulatedState.Boards.Count; boardIndex += 1) {
                    var board = simulatedState.GetBoardByIndex(boardIndex);
                    foreach(var spaceEnumerator in board.AsSpaceEnumerable()) {
                        if (spaceEnumerator.Space.Mark == null) {
                            action = factoryForBoard.Create(boardIndex);
                            actionAssessments.Add(
                                SimulateAction(CloneGameState(simulatedState), action, currentPlayer, objectivePlayer)
                            );
                        }
                    }
                }
            }
            if (factory is GameActionFactoryForRow) {
                //TODO GameActionFactoryForRow
                throw new NotImplementedException();
            }
            if (factory is GameActionFactoryForColumn) {
                //TODO GameActionFactoryForColumn
                throw new NotImplementedException();
            }
        }
        
        foreach (var actionAssessment in actionAssessments) {
            if (!actionAssessment.SimulatedState.IsGameOver && depth < MaxDepth) {
                var nextPlayer = actionAssessment.SimulatedState.PlayersState.PlayersAvailableForTurn.First();
                var recursiveActionAssessment = SimulateTurn(actionAssessment.SimulatedState, nextPlayer, objectivePlayer, depth + 1);
                if (recursiveActionAssessment != null) {
                    actionAssessment.Rating += recursiveActionAssessment.Rating;
                }
            }
        }

        var optimalActionAssessments = (
            (currentPlayer == objectivePlayer)
                ? actionAssessments.AllMaxBy(assessment => assessment.Rating)
                : actionAssessments.AllMinBy(assessment => assessment.Rating)
        ).ToList();
        
        //given a tie, choose randomly.
        var optimalActionAssessment = optimalActionAssessments.Count == 1
            ? optimalActionAssessments.Single()
            : optimalActionAssessments[_random.Next(0, optimalActionAssessments.Count)];

        return optimalActionAssessment;
	}

	private ActionAssessment SimulateAction(
        GameState simulatedState,
        GameAction gameAction,
        Player currentPlayer,
        Player objectivePlayer
    ) {
        var baseRating = RateScore(simulatedState.ScoreCard, objectivePlayer);
		simulatedState.Attempt(new PlayerAction(gameAction, currentPlayer));
        if(simulatedState.PlayersState.IsRoundOver) {
            simulatedState.EndRound(out _);
        }
        var newRating = RateScore(simulatedState.ScoreCard, objectivePlayer);
        var resultRating = newRating - baseRating;
        return new ActionAssessment(simulatedState, gameAction, resultRating);
	}

	private GameState CloneGameState(GameView originalStateView) {
        var playersState = new PlayersState(originalStateView.PlayersState);
        var gameTemplate = originalStateView.GameTemplate;
        
        // Create a fresh copy of the game state
        var clonedState = new GameState(playersState, gameTemplate);
        foreach(var boardView in originalStateView.Boards) {
            foreach(var spaceView in boardView.AsSpaceViewEnumerable()) {
                var clonedSpace = clonedState.Boards[boardView.BoardIndex].Spaces[spaceView.Col, spaceView.Row];
                clonedSpace.Mark = boardView.Spaces[spaceView.Col, spaceView.Row].Mark;
                clonedSpace.MakeKnownToPlayer(originalStateView.Player!);
                if(clonedSpace.Mark != null) {
                    clonedSpace.MakeKnownToPlayer(new Player(clonedSpace.Mark!));
                }
            }
        }
        
        return clonedState;
    }

    private GameState CloneGameState(GameState originalState)
    => new GameState(originalState);

    public int RateScore(ScoreCard scoreCard, Player player)
    => scoreCard.PlayerScores.SingleOrDefault(ps => ps.Player == player).Score //playerscore is a struct so default score = 0;
    - scoreCard.PlayerScores.Where(ps => ps.Player != player).Max(ps => ps.Score);
}

internal class ActionAssessment {
    public ActionAssessment(GameState simulatedState, GameAction gameAction, int rating) {
        SimulatedState = simulatedState;
        GameAction = gameAction;
        Rating = rating;
    }
    public GameState SimulatedState;
    public GameAction GameAction;
    public int Rating;
}