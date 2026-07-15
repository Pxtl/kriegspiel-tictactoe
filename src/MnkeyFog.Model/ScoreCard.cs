namespace MnkeyFog.Model;

using System.ComponentModel;
using PxtlCa.Collections;
using PxtlCa.Collections.Extensions;

/// <summary>
/// Immutable value-y collection of scores
/// </summary>
[ModelSerializable]
[ImmutableObject(true)]
public readonly struct ScoreCard {
    #region constructors
    public ScoreCard() {
        _scores = _emptyPlayerScoreCollection;
    }
    public ScoreCard(int playerIndex, int score) : this(new PlayerIndexScore(playerIndex, score)) { }
    public ScoreCard(PlayerIndexScore playerScore) : this([playerScore]) { }
    public ScoreCard(IEnumerable<PlayerIndexScore> scores) {
        _scores = scores
            .GroupBy(s => s.PlayerIndex)
            .OrderBy(g => g.Key)
            .Select(g => new PlayerIndexScore(g.Key, g.Sum(kvp => kvp.Score)))
            .ToStructList();
    }

    /// <summary>
    ///  that is only allowed when we already know scores are
    /// unique.
    /// </summary>
    internal ScoreCard(PlayerIndexScore[] scores) {
        _scores = new StructList<PlayerIndexScore>(scores);
    }
    #endregion

    #region static Empty
    private static StructList<PlayerIndexScore> _emptyPlayerScoreCollection = new StructList<PlayerIndexScore>();
    public static ScoreCard Empty { get; } = new ScoreCard();

    #endregion

    #region state members
    private StructList<PlayerIndexScore> _scores { get; init; }
    public readonly IReadOnlyList<PlayerIndexScore> PlayerScores
    => _scores;
    #endregion

    #region calculated members
    [JsonIgnore]
    public readonly bool IsEmpty
    => _scores.Count == 0;

    [JsonIgnore]
    public readonly ScoreCard Highest
    => _scores.Count == 0
        ? Empty
        : new ScoreCard(_scores.AllMaxBy(s => s.Score));

    public readonly IEnumerable<PlayerInfo> AsPlayerInfos(PlayersState playersState)
    => _scores.Select(s => playersState.PlayerInfos[s.PlayerIndex]);
    #endregion

    #region object overrides (equality and tostring)
    public override string? ToString() => _scores.ToString();

    public override bool Equals(object? obj) {
        if (obj == null) {
            return false;
        } else if (obj is PlayerIndexScore playerScore) {
            obj = new ScoreCard(playerScore);
        }

        if (obj is ScoreCard scoreCard) {
            return _scores.Equals(scoreCard._scores);
        } else {
            return false;
        }
    }

    // override object.GetHashCode
    public override int GetHashCode()
    => _scores.GetHashCode();

    public static bool operator ==(ScoreCard? a, ScoreCard? b) {
        if (a == null) {
            return b == null;
        } else {
            return a.Equals(b);
        }
    }

    public static bool operator !=(ScoreCard? a, ScoreCard? b) {
        return !(a == b);
    }
    #endregion

    public ScoreCard FilterByPlayerIndices(IEnumerable<int> playerIndices)
    => new ScoreCard(PlayerScores.Where(ps => playerIndices.Contains(ps.PlayerIndex)));

    #region operator overloads
    public static ScoreCard operator +(ScoreCard a, ScoreCard b)
    => a.IsEmpty ? b // optimization, if a or b are empty just use the other one directly.
        : b.IsEmpty ? a
        : new ScoreCard(a._scores.Concat(b._scores));

    public static ScoreCard operator +(ScoreCard a, PlayerIndexScore b)
    => new ScoreCard(a._scores.Append(b));

    public static ScoreCard operator +(PlayerIndexScore a, ScoreCard b)
    => new ScoreCard(a) + b;
    #endregion

    public static ScoreCard SumScoreCards(IEnumerable<ScoreCard> scoreCards)
    => new ScoreCard(scoreCards.SelectMany(s => s._scores));
}

/// <summary>
/// Allow the above static <see cref="ScoreCard.SumScoreCards(IEnumerable{ScoreCard})"/> to be used as extension.
/// </summary>
public static class ScoreCardExtensions {
    /// <summary>
    /// can't use ienumerable.sum on non-numeric objects, operators don't work
    /// that way, so we have to create a sum method.
    /// </summary>
    public static ScoreCard SumScoreCards(this IEnumerable<ScoreCard> scoreCards)
    => ScoreCard.SumScoreCards(scoreCards);

    public static ScoreCard BlankPlayersScoreCard(this IReadOnlyList<PlayerInfo> playerInfos) {
        // switch to array-based for performance.
        var playerScores = new PlayerIndexScore[playerInfos.Count];
        for (int i = 0; i < playerInfos.Count; i += 1) {
            playerScores[i] = new PlayerIndexScore(i, 0);
        }
        return new ScoreCard(playerScores);
    }
}
