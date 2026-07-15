using System.Linq;
namespace MnkeyFog.Model.Tests;

internal static class TestExtensions {
    internal static PlayerInfo[] ToPlayersArray(this char[] chars)
        => chars.Select(c => new PlayerInfo(c.ToString())).ToArray();
}
