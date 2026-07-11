namespace MnkeyFog.Model;

using System.ComponentModel;
using OneOf;
using OneOf.Types;

/// <summary>
/// Base class containing shared play management logic for retirement and turn tracking.
/// </summary>
[ModelSerializable]
[ImmutableObject(true)] //not read by anything just useful metadata.
public abstract class PlayManager {
    public abstract string GameStateText(PlayersState playerState);
    public abstract void EndedRound(GameState gameState, out bool hasStateChanged);
    public abstract void EndedTurn(GameState gameState, out bool hasStateChanged);
    public abstract IEnumerable<PlayerIndexed> PlayersAvailableForTurn(PlayersState playerState);
}