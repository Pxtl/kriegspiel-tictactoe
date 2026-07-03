using System.ComponentModel;

namespace MnkeyFog.Model;

/// <summary>
/// Score for a single player
/// </summary>
[ImmutableObject(true)]
[ModelSerializable]
public readonly record struct PlayerScore(Player Player, int Score) {
    public static implicit operator ScoreCard(PlayerScore p) => new ScoreCard(p);   
}
