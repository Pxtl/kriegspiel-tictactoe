using System.ComponentModel;

namespace MnkeyFog.Model;

/// <summary>
/// Represents a player in the game. Stores their marker string.
/// </summary>
[ModelSerializable]
[ImmutableObject(true)]
public sealed record PlayerInfo {
    public PlayerInfo(string mark) {
        if (mark == null) {
            throw new ArgumentNullException("Mark must be provided", nameof(mark));
        } else if (mark.Length != 1) {
            throw new ArgumentException("Mark must be length 1", nameof(mark));
        } else if (string.IsNullOrWhiteSpace(mark)) {
            throw new ArgumentException("Mark must not be whitespace.", nameof(mark));
        } else if (char.IsControl(mark[0])) {
            throw new ArgumentException("Mark must not be a control char.", nameof(mark));
        }
        Mark = mark;
    }
    public string Mark { get; init; }

    public override string ToString()
        => Mark;

    /// <summary>
    /// Create a Player from a char.
    /// </summary>
    public static PlayerInfo FromChar(char value) => new PlayerInfo(value.ToString());

    /// <summary>
    /// Create a Player from a 1-character nullable string.  Returns null if the
    /// parameter is not a 1-character string.
    /// </summary>
    public static PlayerInfo? FromString(string? value) {
        if (value == null) {
            return null;
        } else if (value.Length != 1) {
            return null;
        }
        return new PlayerInfo(value);
    }

    /// <summary>
    /// Implicit conversion from char string to Player.
    /// </summary>
    public static implicit operator PlayerInfo(string value) => new PlayerInfo(value);

    /// <summary>
    /// Implicit conversion from char to Player.
    /// </summary>
    public static implicit operator PlayerInfo(char value) => new PlayerInfo(value.ToString());

    /// <summary>
    /// Implicit conversion from Player to string.
    /// </summary>
    public static implicit operator string(PlayerInfo player) => player.Mark;
}
