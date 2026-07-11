namespace MnkeyFog.Model;

/// <summary>
/// Carries info about a player.  Do not serialize.
/// </summary>
public record Player(PlayerInfo Info, int Index) {
    public Player(string mark, int index) 
    : this(new PlayerInfo(mark), index) { }
    
    [JsonIgnore]
    public string Mark => Info.Mark;
}