namespace MnkeyFog.Model.Indexed;

/// <summary>
/// Carries info about a player.  Do not serialize.
/// </summary>
public record PlayerIndexed(PlayerInfo Info, int Index) {
    public PlayerIndexed(string mark, int index) 
    : this(new PlayerInfo(mark), index) { }
    
    [JsonIgnore]
    public string Mark => Info.Mark;
}