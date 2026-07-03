namespace MnkeyFog.Model;

using OneOf;
using OneOf.Types;

/// <summary>
/// Base class containing shared play management logic for retirement and turn tracking.
/// </summary>
[ModelSerializable]
public class PlayersState {
    #region constructor
    public PlayersState(IReadOnlyList<Player> players, PlayManager playManager) {
        Players = players;
        PlayManager = playManager;
    }

	public PlayersState(PlayersState playersState) {
        playersState.PlayManager.ConfirmHasImmutableAttribute();

        Players = playersState.Players;
        PlayManager = playersState.PlayManager;
        RoundIndex = playersState.RoundIndex;
        ResignedPlayersSet = new HashSet<Player>(playersState.ResignedPlayersSet);
        PlayedPlayersSet = new HashSet<Player>(playersState.PlayedPlayersSet);
	}

	#endregion

	#region data members
	[JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.
    public IReadOnlyList<Player> Players {
        get; init {
            // Validation: ToDictionary will throw ArgumentException on non-unique key.
            _ = value
                .ToDictionary(p => p.Mark, StringComparer.OrdinalIgnoreCase);

            field = value;
        }
    } = new List<Player>();

    /// <summary>
    /// Play-manager is the pluggable player-turn-order management system.
    /// </summary>
    public PlayManager PlayManager { get; init; }

    public int RoundIndex { get; set; }

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.

    public HashSet<Player> ResignedPlayersSet { get; init; } = new HashSet<Player>();
    
    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.

    public HashSet<Player> PlayedPlayersSet { get; init; } = new HashSet<Player>();
    #endregion

    #region methods
    /// <summary>
    /// Advance to the next player's turn.  Must notify if current player is
    /// resigning because that effects how the turn counter is incremented.
    /// </summary>
    /// <remarks>
    /// The logic here is tricky. If the current player resigns, then their slot
    /// is removed from the index and we skip incrementing.  But that means
    /// there are 2 "index 0" turns, so we can't use "index 0" as new round in
    /// that case.
    /// </remarks>
    public void EndTurn(GameState gameState, Player currentPlayer, out bool hasStateChanged) {
        MarkPlayerPlayed(currentPlayer);
        PlayManager.EndedTurn(gameState, out hasStateChanged);
    }

    public void MarkPlayerPlayed(Player player) {
        if (PlayedPlayersSet.Contains(player)) {
            throw new InvalidOperationException($"Player {player} has already played");
        }
        PlayedPlayersSet.Add(player);
    }

    public void EndRound(GameState gameState, out bool hasStateChanged) {
        RoundIndex += 1;
        PlayedPlayersSet.Clear();
        PlayManager.EndedRound(gameState, out hasStateChanged);
    }
    
    /// <summary>
    /// Test if the given player has resigned.
    /// </summary>
    public bool IsResignedPlayer(Player player)
    => ResignedPlayersSet.Contains(player);

    /// <summary>
    /// Mark the given player as resigned. If it is the current player's turn,
    /// do *not* call NextTurn.
    /// </summary>
    public void ResignPlayer(Player player) {
        ResignedPlayersSet.Add(player);
    }

    /// <summary>
    /// True if the given player is able to take a turn.
    /// </summary>
    public bool CanTakeTurn(Player? player)
        => player != null && PlayersAvailableForTurn.Contains(player);
    #endregion

    #region helper properties
    [JsonIgnore()]
    public int NumberOfActivePlayers
    => ActivePlayers.Count();
    
    /// <summary>
    /// Get all of the current active players.  Order is consistent.
    /// </summary>
    [JsonIgnore()]
    public IEnumerable<Player> ActivePlayers
    => Players.Except(ResignedPlayersSet);

    [JsonIgnore()]
    public IEnumerable<Player> PlayersAvailableForTurn
    => PlayManager.PlayersAvailableForTurn(this);

    [JsonIgnore()]
    public bool IsRoundOver
    => PlayersAvailableForTurn.Count() == 0;

    /// <summary>
    /// Abstract GameStateText property - implemented by subclasses.
    /// </summary>
    [JsonIgnore()]
    public string GameStateText 
    => PlayManager.GameStateText(this);
    #endregion
}
