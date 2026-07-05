namespace MnkeyFog.Model;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using MnkeyFog.Model.Indexed;
using OneOf;
using OneOf.Types;

/// <summary>
/// Base class containing shared play management logic for retirement and turn tracking.
/// </summary>
[ModelSerializable]
public class PlayersState {
    #region constructor
    [Obsolete(
        "Empty constructor puts members in invalid state needs them to be replaced by initializers."
    )]
    public PlayersState() {
        Players = [];
        PlayManager = RoundRobinPlayManager.Instance;
    }

    public PlayersState(IReadOnlyList<Player> players, PlayManager playManager)
    : this(players, playManager, isRandomPlayerOrder: false) {}

    public PlayersState(IReadOnlyList<Player> players, PlayManager playManager, bool isRandomPlayerOrder) {
        if (isRandomPlayerOrder) {
            players = players.Shuffle().ToList();
        }
        Players = players;
        PlayManager = playManager;
    }

    /// <summary>
    /// Copy-constructor.
    /// </summary>
	public PlayersState(PlayersState playersState) {
        playersState.PlayManager.ConfirmHasImmutableAttribute();

        Players = playersState.Players;
        PlayManager = playersState.PlayManager;
        RoundIndex = playersState.RoundIndex;
        ResignedPlayerIndicesSet = new HashSet<int>(playersState.ResignedPlayerIndicesSet);
        PlayedPlayerIndicesSet = new HashSet<int>(playersState.PlayedPlayerIndicesSet);
	}

	#endregion

	#region data members
    [MemberNotNull(nameof(_indicesByPlayer))]
    [MemberNotNull(nameof(_playerIndicesSet))]
	[JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.
    public IReadOnlyList<Player> Players {
        get; init {
            // Validation: ToDictionary will throw ArgumentException on non-unique key.
            _ = value.ToDictionary(p => p.Mark, StringComparer.OrdinalIgnoreCase);

            var indicesByPlayer = new KeyValuePair<Player, int>[value.Count];
            for(var i = 0; i < value.Count; i += 1) {
                indicesByPlayer[i] = new KeyValuePair<Player, int>(value[i], i);
            }
           
            _indicesByPlayer = indicesByPlayer.ToImmutableDictionary();
            _playerIndicesSet = Enumerable.Range(0, value.Count).ToImmutableHashSet();

            field = value;
        }
    } = new List<Player>();
    private ImmutableDictionary<Player, int> _indicesByPlayer;
    private ImmutableHashSet<int> _playerIndicesSet;

    /// <summary>
    /// Play-manager is the pluggable player-turn-order management system.
    /// </summary>
    public PlayManager PlayManager { get; init; }

    public int RoundIndex { get; set; }

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.

    public HashSet<int> ResignedPlayerIndicesSet { get; init; } = new HashSet<int>();
    
    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)] //non-polymorphic.

    public HashSet<int> PlayedPlayerIndicesSet { get; init; } = new HashSet<int>();
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
    public void EndTurn(GameState gameState, PlayerIndexed currentPlayer, out bool hasStateChanged) {
        MarkPlayerPlayed(currentPlayer);
        PlayManager.EndedTurn(gameState, out hasStateChanged);
    }

    public void MarkPlayerPlayed(PlayerIndexed playerIndexed) {
        if (PlayedPlayerIndicesSet.Contains(playerIndexed.Index)) {
            throw new InvalidOperationException($"Player {playerIndexed.Player} has already played");
        }
        PlayedPlayerIndicesSet.Add(playerIndexed.Index);
    }

    public void EndRound(GameState gameState, out bool hasStateChanged) {
        RoundIndex += 1;
        PlayedPlayerIndicesSet.Clear();
        PlayManager.EndedRound(gameState, out hasStateChanged);
    }
    
    /// <summary>
    /// Test if the given player has resigned.
    /// </summary>
    public bool IsResignedPlayer(PlayerIndexed player)
    => IsResignedPlayer(player.Index);

    public bool IsResignedPlayer(int playerIndex)
    => ResignedPlayerIndicesSet.Contains(playerIndex);

    /// <summary>
    /// Mark the given player as resigned. If it is the current player's turn,
    /// do *not* call NextTurn.
    /// </summary>
    public void ResignPlayer(PlayerIndexed player) {
        ResignPlayer(player.Index);
    }

    public void ResignPlayer(int playerIndex) {
        ResignedPlayerIndicesSet.Add(playerIndex);
    }

    /// <summary>
    /// True if the given player is able to take a turn.
    /// </summary>
    public bool CanTakeTurn(int? playerIndex)
    => playerIndex != null && PlayersAvailableForTurn.Any(p => p.Index == playerIndex);

    public string GetMark(int? markIndex) {
        if(markIndex == Space.ImpasseMarkIndex) {
            return Space.ImpasseChar.ToString();
        } else if(markIndex.HasValue) {
            return Players[markIndex.Value].Mark;
        } else {
            return Space.EmptyMarkString;
        }
    }

    public PlayerIndexed GetPlayerIndexed(int playerIndex) 
    => new PlayerIndexed(Players[playerIndex], playerIndex);

    public PlayerIndexed GetPlayerIndexed(string mark) 
    => GetPlayerIndexed(new Player(mark));

    public PlayerIndexed GetPlayerIndexed(Player player)
    => new PlayerIndexed(player, _indicesByPlayer[player]);
    #endregion

    #region helper properties
    [JsonIgnore()]
    public IEnumerable<int> PlayerIndices => Enumerable.Range(0, Players.Count);

    [JsonIgnore()]
    public IEnumerable<PlayerIndexed> PlayersIndexed { get {
        for (var i = 0; i < Players.Count; i+=1) {
            yield return new PlayerIndexed(Players[i], i);
        }
    } }

    [JsonIgnore()]
    public int NumberOfActivePlayers
    => ActivePlayers.Count();

    [JsonIgnore()]
    public ImmutableHashSet<int> ActivePlayerIndices
    => _playerIndicesSet.Except(ResignedPlayerIndicesSet);
    
    /// <summary>
    /// Get all of the current active players.  Order is consistent.
    /// </summary>
    [JsonIgnore()]
    public IEnumerable<PlayerIndexed> ActivePlayers
    => PlayersIndexed.Where(p => ActivePlayerIndices.Contains(p.Index));

    [JsonIgnore()]
    public IEnumerable<PlayerIndexed> PlayersAvailableForTurn
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
