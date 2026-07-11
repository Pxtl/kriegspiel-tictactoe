namespace MnkeyFog.Model;

using System.ComponentModel;

/// <summary>
/// PlayManager for synchronized mode - player moves are buffered until round end.
/// </summary>
[ModelSerializable]
[ImmutableObject(true)]
public class SynchronizedPlayManager 
: PlayManager {
    public static SynchronizedPlayManager Instance {get;} = new SynchronizedPlayManager();

    public override string GameStateText(PlayersState playerState)
        => "Synchronized play. "
        + (playerState.PlayersAvailableForTurn.Any()
            ? $"Player(s) { string.Join(", ", playerState.PlayersAvailableForTurn)} have not taken their turn."
            : "Round complete."
        );

    public override void EndedRound(GameState gameState, out bool hasStateChanged) {
        gameState.ExecutePendingActions();
        hasStateChanged = true;
    }

    public override void EndedTurn(GameState gameState, out bool hasStateChanged) {
        hasStateChanged = false;
    }
    
    public override IEnumerable<Player> PlayersAvailableForTurn(PlayersState playerState)
        => playerState.ActivePlayers.Where(p => !playerState.PlayedPlayerIndicesSet.Contains(p.Index));
}