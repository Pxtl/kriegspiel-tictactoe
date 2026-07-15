using System.ComponentModel.DataAnnotations;
using MnkeyFog.Model.Indexed;

namespace MnkeyFog.Model;

/// <summary>
/// JSON-serializable model object for a single tic-tac-toe board. Columns are
/// left-to-right, rows are top-to-bottom.
/// </summary>
[ModelSerializable]
public sealed record Board {
    #region constructors
    /// <summary>
    /// Default constructor creates a useless board.  Never uses this without
    /// replacing <see cref="Ruleset"> and <see cref="Spaces"/> members.
    /// </summary>
    public Board()
    : this(1, 1, BoardRuleset.Empty) { }

    public Board(Template.BoardBuilder builder)
    : this(builder.ColumnCount, builder.RowCount, builder.Ruleset) { }

    public Board(sbyte columnCount, sbyte rowCount)
    : this(columnCount, rowCount, null) { }

    public Board(sbyte columnCount, sbyte rowCount, BoardRuleset? ruleset) {
        Ruleset = ruleset ?? BoardRuleset.Empty;
        Spaces = new Space[columnCount, rowCount];
        for (sbyte col = 0; col < ColumnCount; col += 1) {
            for (sbyte row = 0; row < RowCount; row += 1) {
                Spaces[col, row] = new Space();
            }
        }
    }

    public Board(Board board) {
        board.Ruleset.ConfirmHasImmutableAttribute();
        Ruleset = board.Ruleset;
        Spaces = new Space[board.ColumnCount, board.RowCount];
        foreach (var spaceEnumerator in board.AsSpaceEnumerable()) {
            Spaces[spaceEnumerator.Col, spaceEnumerator.Row] = new Space(spaceEnumerator.Space);
        }
    }
    #endregion

    #region data members
    [Required]
    [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None, TypeNameHandling = TypeNameHandling.None)] //non-polymorphic
    public Space[,] Spaces { get; init; }
    [Required]
    public BoardRuleset Ruleset { get; init; }
    #endregion

    #region Methods
    public IEnumerable<SpaceIndexed> AsSpaceEnumerable() {
        for (sbyte col = 0; col < Spaces.GetLength(0); col += 1) {
            for (sbyte row = 0; row < Spaces.GetLength(1); row += 1) {
                yield return new SpaceIndexed(Spaces[col, row], col, row);
            }
        }
    }

    /// <summary>
    /// Returns true if space pos is within this board.
    /// </summary>
    public bool IsSpaceInsideOfBoard((sbyte Col, sbyte Row) pos)
    => IsSpaceInsideOfBoard(pos, (ColumnCount, RowCount));

    /// <summary>
    /// Returns true if a space is within an arbitrarily-sized board.
    /// </summary>
    public static bool IsSpaceInsideOfBoard((sbyte Col, sbyte Row) pos, (sbyte Col, sbyte Row) boardSize)
    => (pos.Col < boardSize.Col)
        && (pos.Row < boardSize.Row)
        && (pos.Col >= 0)
        && (pos.Row >= 0);
    #endregion

    #region ruleset
    /// <summary>
    /// The scores for this board. Value is only updated when <see
    /// cref="ExecuteRuleset"/> is called, which happens at the end of action
    /// queue processing.
    /// </summary>
    public ScoreCard ScoreCard { get; private set; }

    /// <summary>
    /// Returns true if the board is done and locked from further play. Value is
    /// only updated when <see cref="ExecuteRuleset"/> is called, which happens at the end of action
    /// queue processing.
    /// </summary>
    public bool IsDone { get; private set; }

    public void ExecuteRuleset() {
        ScoreCard = Ruleset.Score(this);
        IsDone = IsFull || Ruleset.IsDone(this);
    }
    #endregion

    #region helper properties
    [JsonIgnore()]
    public sbyte ColumnCount
    => Spaces.GetLength(0).AsSByte;

    [JsonIgnore()]
    public sbyte RowCount
    => Spaces.GetLength(1).AsSByte;

    /// <summary>
    /// Get how many spaces are on the board.
    /// </summary>
    [JsonIgnore()]
    public int SpaceCount
    => Spaces.GetLength(0) * Spaces.GetLength(1);

    /// <summary>
    /// Returns true if the board is full.
    /// </summary>
    [JsonIgnore()]
    public bool IsFull
    => AsSpaceEnumerable().All(s => s.Space.MarkIndex != null);
    #endregion
}
