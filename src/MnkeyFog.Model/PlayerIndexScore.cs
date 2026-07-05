using System.ComponentModel;

namespace MnkeyFog.Model;

/// <summary>
/// Score for a single player
/// </summary>
[ImmutableObject(true)]
[ModelSerializable]
public readonly record struct PlayerIndexScore(int PlayerIndex, int Score) {
    public static implicit operator ScoreCard(PlayerIndexScore p) => new ScoreCard(p);   
}
