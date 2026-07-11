using System.ComponentModel;

namespace MnkeyFog.Model;

[ModelSerializable]
[ImmutableObject(true)]
public abstract record GameAction() {
    public abstract IPlayActionResult Attempt(GameState gameState, int actionPlayerIndex);
    public abstract void DoAction(GameState gameState, int actionPlayer);
    public abstract bool IsActionCollision(PlayerAction otherAction, int actionPlayer);
    public abstract void DoActionCollision(GameState gameState, int actionPlayer, IReadOnlyList<PlayerAction> collisions);
    public PlayerAction GetPlayerAction(int playerIndex) => new PlayerAction(this, playerIndex);

    public IPlayActionResult Attempt(GameState gameState, Player actionPlayerInfo)
    => Attempt(gameState, actionPlayerInfo.Index);
}
