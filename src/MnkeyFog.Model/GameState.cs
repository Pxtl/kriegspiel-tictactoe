using System.Runtime.Serialization;
using MnkeyFog.Model.Template;
using MnkeyFog.Model.Views;

namespace MnkeyFog.Model;

[ModelSerializable]
public class GameState
: IGameState, IGameStateServer {
    #region Constructors
    public GameState() { 
        // unusable default values will probably get removed when members are
        // initialized.
        PlayersState = new PlayersState([], RoundRobinPlayManager.Instance);
        Boards = [];
        GameTemplate = null!;
    }

    /// <summary>
    /// Copy-constructor.
    /// </summary>
    public GameState(GameState gameState) {
        gameState.GameTemplate.ConfirmHasImmutableAttribute();
        
        Boards = gameState.Boards.Select(board => new Board(board)).ToList();
        PlayersState = new PlayersState(gameState.PlayersState);
        GameTemplate = gameState.GameTemplate;
        ActionQueue = new PlayActionQueue(gameState.ActionQueue);
    }

    public GameState(
        Player[] players,
        IGameTemplate gameTemplate,
        bool isRandomPlayerOrder
    ) : this(new PlayersState(players, gameTemplate.PlayManager, isRandomPlayerOrder), gameTemplate) { }

    public GameState(
        PlayersState playersState,
        IGameTemplate gameTemplate
    ) {
        GameTemplate = gameTemplate;
        PlayersState = playersState;
        Boards = gameTemplate.CreateBoards();

        if (Boards.Count > 1 && Boards.Any(b => b.RowCount > 9)) {
            throw new ApplicationException(
                "Cannot start game. Current board-renderer does not support boards that are taller than 9 spaces in multi-board games."
            );
        }

        if (!gameTemplate.LegalPlayerCounts.Contains(PlayersState.Players.Count)) {
            throw new ApplicationException(
                "Cannot start game. This game only supports the following player-counts: "
                    + string.Join(", ", gameTemplate.LegalPlayerCounts)
                    + Environment.NewLine
                    + $"You have provided {PlayersState.Players.Count} player(s)."
            );
        }

        gameTemplate.InitializeGame(this);
    }
    #endregion

    #region Data Properties
    [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
    public IGameTemplate GameTemplate { get; init; }

    [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
    public virtual PlayersState PlayersState { get; init; }

    [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
    public virtual IReadOnlyList<Board> Boards { get; init; }

    [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
    public PlayActionQueue ActionQueue { get; init; } = new();
    #endregion

    #region Actions

    public IPlayActionResult Attempt(PlayerAction action) => action.Attempt(this);
    public void ResignPlayer(Player player) => PlayersState.ResignPlayer(player);

    public void ExecutePendingActions()
    => ActionQueue.ExecutePendingActions(this);

    public void EndTurn(Player player, out bool hasStateChanged)
    => PlayersState.EndTurn(this, player, out hasStateChanged);

    public void EndRound(out bool hasRoundStateChanged)
    => PlayersState.EndRound(this, out hasRoundStateChanged);

    #endregion

    public GameView GetView(Player? player)
    => new(this, player);

    #region Players and Scores
    /// <summary>
    /// Scorecard for all active (non-resigned) players
    /// </summary>
    [JsonIgnore()]
    public ScoreCard ScoreCard 
    => AllPlayersScoreCard.FilterByPlayers(PlayersState.ActivePlayers);

    
    [JsonIgnore()]
    public ScoreCard AllPlayersScoreCard
    => PlayersState.Players.BlankPlayersScoreCard()  //make sure all active players are in the scorecard even those with 0.
        + Boards.Select(b => b.ScoreCard).SumScoreCards();


    [JsonIgnore()]
    public virtual IEnumerable<Player> Winners { get {
        if(!IsGameOver) {
            return [];
        }
        if(PlayersState.ActivePlayers.Count() == 1) {
            return PlayersState.ActivePlayers;
        }
        else {
            return ScoreCard.Highest.Players;
        }
    }}

    [JsonIgnore()]
    public bool IsGameOver
    => Boards.All(b => b.IsDone) || PlayersState.ActivePlayers.Count() == 1;


    [JsonIgnore()]
    public string GameStateText
    => (
            IsGameOver 
            ? (Winners.Count() == 0
                ? "Game over. Nobody wins."
                : $"Game over. {string.Join(" and ", Winners)} win(s)."
            ) 
            : PlayersState.GameStateText
        )
            + Environment.NewLine 
            + ResignedPlayersText;

    [JsonIgnore()]
    public string ResignedPlayersText
    => PlayersState.ResignedPlayersSet.Count > 0
        ? $"Resigned players: {string.Join(", ", PlayersState.ResignedPlayersSet.OrderBy(p => p.Mark))}"
        : "";
    #endregion

    #region board management

    [JsonIgnore()]
    public IEnumerable<sbyte> ActiveBoardIndices { get {
        for(sbyte i = 0; i < Boards.Count; i+=1) {
            if(!Boards[i].IsDone) {
                yield return i; 
            }
        }
    }}
    
    /// <summary>
    /// If there is only one active board, return its index.  Otherwise, return
    /// null.
    /// </summary>
    [JsonIgnore()]
    public sbyte? SingleActiveBoardIndex { get {
        var firstElements = ActiveBoardIndices.Take(2).ToArray();
        return (firstElements.Length == 1) ? firstElements.Single() : null;
    }}

    public Board GetBoardByIndex(sbyte boardIndex)
    => Boards[boardIndex];

	#endregion
}
