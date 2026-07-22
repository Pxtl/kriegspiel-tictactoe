using System.Linq;
namespace MnkeyFog.Model.Tests;

internal static class TestExtensions {
    internal static PlayerInfo[] ToPlayersArray(this char[] chars)
        => chars.Select(c => new PlayerInfo(c.ToString())).ToArray();

    /// <summary>
    /// Standardizes line endings and trims trailing whitespace.
    /// </summary>
    internal static string NormalizeString(this string str)
        => str.ReplaceLineEndings().TrimEnd();

    /// <summary>
    /// When using @ strings, it's useful to start the string with a linebreak.
    /// This function trims that off.
    /// </summary>
    internal static string RemoveStartingBreak(this string str)
        => str.StartsWith("\r\n")
        ? str.Substring(2)
        : str.StartsWith("\n")
        ? str.Substring(1)
        : str;
}
