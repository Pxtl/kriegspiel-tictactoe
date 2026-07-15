using System.ComponentModel;

namespace MnkeyFog.Model;

/// <summary>
/// Parameter and logic for scoring a board. Default implementation gives nobody
/// any points ever, you must override Score to get actual useful gameplay.
/// </summary>
[ModelSerializable]
[ImmutableObject(true)]
public record BoardRuleset {
    public static BoardRuleset Empty { get; } = new BoardRuleset();
    public virtual bool IsDone(Board board)
    => false;

    public virtual ScoreCard Score(Board board)
    => ScoreCard.Empty;

    /// <summary>
    /// When constructing a BoardView, does the player know the true score or
    /// only their personal score?  Use this to control that. If players need
    /// the true score, redirect to Score(board).
    /// </summary>
    public virtual ScoreCard Score(Board board, int? playerIndex)
    => Score(board);

    public override string ToString()
    => "empty rules (scoring is impossible)";
};
