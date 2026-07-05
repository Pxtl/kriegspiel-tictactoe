namespace MnkeyFog.Model.Indexed;

/// <summary>
/// Carries info about a player.  Do not serialize.
/// </summary>
public record PlayerIndexed(Player Player, int Index) {
    public PlayerIndexed(string mark, int index) 
    : this(new Player(mark), index) { }
    
    [JsonIgnore]
    public string Mark => Player.Mark;
}