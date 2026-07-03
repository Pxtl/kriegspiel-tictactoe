using MnkeyFog.Model.Template;
using MnkeyFog.Model.Views;

namespace MnkeyFog.Model.PlayerAIs;

[ModelSerializable]
public class MontyAI : IPlayerAI {
    public string Description => "Monty, difficulty 4";
    private const int MaxDepth = 5;

    public void Attempt(GameView gameView, IEnumerable<GameActionFactory> actionFactories) {             
        var simulatedState = CloneGameState(gameView);
        var depth = 0;
        var optimalActionAssessment = SimulateTurn(simulatedState, gameView.Player!, gameView.Player!, depth);
        if(optimalActionAssessment != null) {
            gameView.Attempt(optimalActionAssessment.GameAction);
        }
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
                var nextPlayer = actionAssessment.SimulatedState.PlayManager.PlayersAvailableForTurn.First();
                var recursiveActionAssessment = SimulateTurn(actionAssessment.SimulatedState, nextPlayer, objectivePlayer, depth + 1);
                if (recursiveActionAssessment != null) {
                    actionAssessment.Rating += recursiveActionAssessment.Rating;
                }
            }
        }

        var optimalActionAssessment = (currentPlayer == objectivePlayer)
            ? actionAssessments.MaxBy(assessment => assessment.Rating)
            : actionAssessments.MinBy(assessment => assessment.Rating);

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
        if(simulatedState.PlayManager.IsRoundOver) {
            simulatedState.PlayManager.EndRound(out _);
        }
        var newRating = RateScore(simulatedState.ScoreCard, objectivePlayer);
        var resultRating = newRating - baseRating;
        return new ActionAssessment(simulatedState, gameAction, resultRating);
	}

    //TODO: We need the new playerManager to know which players have finished
    //their turns.  That's not accessible in GameView.
	private GameState CloneGameState(GameView originalStateView) {
        var players = originalStateView.AllPlayers;
        var gameTemplate = originalStateView.GameTemplate;
        
        // We don't need to copy the IsRandomPlayerOrder property because the
        // players collection have copied the randomized order, if any.
        
        // Create a fresh copy of the game state
        var clonedState = new GameState(players.ToArray(), gameTemplate, false);
        foreach(var boardView in originalStateView.Boards) {
            foreach(var spaceView in boardView.AsSpaceViewEnumerable()) {
                var clonedSpace = clonedState.Boards[boardView.BoardIndex].Spaces[spaceView.Col, spaceView.Col];
                clonedSpace.Mark = boardView.Spaces[spaceView.Col, spaceView.Row].Mark;
                clonedSpace.MakeKnownToPlayer(originalStateView.Player!);
                clonedSpace.MakeKnownToPlayer(new Player(clonedSpace.Mark!));
            }
        }
        
        return clonedState;
    }

    //TODO: Implement this into GameState.
    private GameState CloneGameState(GameState originalState) {
        // Create a new GameState copy for Monte Carlo simulation
        var players = originalState.PlayManager.Players;
        var gameTemplate = originalState.GameTemplate;
        
        // We don't need to copy the IsRandomPlayerOrder property because the
        // players collection have copied the randomized order, if any.
        
        // Create a fresh copy of the game state
        var clonedState = new GameState(players.ToArray(), gameTemplate, false);
        for(sbyte boardIndex = 0; boardIndex < originalState.Boards.Count; boardIndex += 1) {
            var board = originalState.GetBoardByIndex(boardIndex);
            foreach(var spaceEnumerator in board.AsSpaceEnumerable()) {
                var clonedSpace = clonedState.Boards[boardIndex].Spaces[spaceEnumerator.Col, spaceEnumerator.Col];
                clonedSpace.Mark = board.Spaces[spaceEnumerator.Col, spaceEnumerator.Row].Mark;
                foreach(var knownToPlayer in spaceEnumerator.Space.KnownToPlayersSet) {
                    clonedSpace.MakeKnownToPlayer(knownToPlayer);
                }
            }
        }
        
        return clonedState;
    }

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