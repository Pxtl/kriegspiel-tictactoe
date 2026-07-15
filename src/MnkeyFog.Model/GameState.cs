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
        PlayerInfo[] players,
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

        if (PlayersState.PlayerInfos.Count > 32) {
            throw new ApplicationException(
                "There is a hard limit of 32 players."
            );
        }

        if (!gameTemplate.LegalPlayerCounts.Contains(PlayersState.PlayerInfos.Count)) {
            throw new ApplicationException(
                "Cannot start game. This game only supports the following player-counts: "
                    + string.Join(", ", gameTemplate.LegalPlayerCounts)
                    + Environment.NewLine
                    + $"You have provided {PlayersState.PlayerInfos.Count} player(s)."
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
    public void ResignPlayer(int playerIndex) => PlayersState.ResignPlayer(playerIndex);

    public void ExecutePendingActions()
    => ActionQueue.ExecutePendingActions(this);

    public void EndTurn(int playerIndex, out bool hasStateChanged)
    => PlayersState.EndTurn(this, PlayersState.GetPlayer(playerIndex), out hasStateChanged);

    public void EndRound(out bool hasRoundStateChanged)
    => PlayersState.EndRound(this, out hasRoundStateChanged);

    #endregion

    public GameView GetSpectatorView()
    => new(this, (int?)null);
    public GameView GetView(Player? player)
    => new(this, player?.Index);
    public GameView GetView(int? playerIndex)
    => new(this, playerIndex);

    #region Players and Scores   
    [JsonIgnore()]
    public ScoreCard ScoreCard
    => PlayersState.PlayerInfos.BlankPlayersScoreCard()  //make sure all active players are in the scorecard even those with 0.
        + Boards.Select(b => b.ScoreCard).SumScoreCards();


    [JsonIgnore()]
    public virtual IEnumerable<PlayerInfo> Winners {
        get {
            if (!IsGameOver) {
                return [];
            }
            var activePlayersScores = new ScoreCard(
                ScoreCard.PlayerScores.Where(playerScore => PlayersState.ActivePlayerIndices.Contains(playerScore.PlayerIndex))
            );
            return activePlayersScores.Highest.AsPlayerInfos(PlayersState);
        }
    }

    [JsonIgnore()]
    public bool IsGameOver
    => Boards.All(b => b.IsDone) || PlayersState.ActivePlayers.Count() == 1;


    [JsonIgnore()]
    public string GameStateText
    => (
            IsGameOver
            ? ((Winners.Count() == 0 || ScoreCard.PlayerScores.All(ps => ps.Score == 0))
                ? "Game over. Nobody wins."
                : $"Game over. {string.Join(" and ", Winners)} win(s)."
            )
            : PlayersState.GameStateText
        )
            + Environment.NewLine
            + ResignedPlayersText;

    [JsonIgnore()]
    public string ResignedPlayersText
    => PlayersState.ResignedPlayerIndicesSet.Count > 0
        ? $"Resigned players: {string.Join(", ", PlayersState.ResignedPlayerIndicesSet.OrderBy(ix => ix).Select(ix => PlayersState.GetPlayer(ix).Info))}"
        : "";
    #endregion

    #region board management

    [JsonIgnore()]
    public IEnumerable<sbyte> ActiveBoardIndices {
        get {
            for (sbyte i = 0; i < Boards.Count; i += 1) {
                if (!Boards[i].IsDone) {
                    yield return i;
                }
            }
        }
    }

    /// <summary>
    /// If there is only one active board, return its index.  Otherwise, return
    /// null.
    /// </summary>
    [JsonIgnore()]
    public sbyte? SingleActiveBoardIndex {
        get {
            var firstElements = ActiveBoardIndices.Take(2).ToArray();
            return (firstElements.Length == 1) ? firstElements.Single() : null;
        }
    }

    public Board GetBoardByIndex(sbyte boardIndex)
    => Boards[boardIndex];

    #endregion
}
