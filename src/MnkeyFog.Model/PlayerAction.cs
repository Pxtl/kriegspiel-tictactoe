using System.ComponentModel;

namespace MnkeyFog.Model;

[ModelSerializable]
[ImmutableObject(true)]
public sealed record PlayerAction(GameAction GameAction, int PlayerIndex) {
    public IPlayActionResult Attempt(GameState gameState) => GameAction.Attempt(gameState, PlayerIndex);
	public bool IsActionCollision(PlayerAction otherAction) => GameAction.IsActionCollision(otherAction, PlayerIndex);
    public void DoAction(GameState gameState) => GameAction.DoAction(gameState, PlayerIndex);
    public void DoActionCollision(GameState gameState, IReadOnlyList<PlayerAction> collisions) 
    => GameAction.DoActionCollision(gameState, PlayerIndex, collisions);
}
