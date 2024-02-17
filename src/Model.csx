#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: OneOf, 3.0.263"

using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using OneOf;
using OneOf.Types;

#region extension methods
/// <summary>
/// Variant of MaxBy that returns NULL if there is not a single clear maximum.
/// </summary>
public static Nullable<TItem> MaxByStrict<TItem, TProperty> (this ICollection<TItem> items, Func<TItem, TProperty> getter) where TItem:struct {
    var maxItem = items.MaxBy(getter);
    var maxPropVal = getter(maxItem);
    var isFound = false;
    if(items.Count == 0) {
        return null;
    }
    foreach (var item in items) {
        var itemPropVal = getter(item);
        if (Object.Equals (itemPropVal, maxPropVal)) {
            if (isFound) {
                return null; //there are 2 or more items with the maxPropVal, max is not distinct.
            } else {
                isFound = true;
            }
        }
    }
    return maxItem;
}
#endregion

/// <summary>
/// class to represent a space on the board.
/// </summary>
public record Space {
    /// <summary>
    /// The current state of the space - NULL means available.
    /// </summary>
    public char? MarkChar {get;set;}
    private HashSet<char> _knownToPlayersSet {get;set;} = new HashSet<char>();
    public IReadOnlySet<char> KnownToPlayersSet => _knownToPlayersSet;
    
    /// <summary>
    /// Test if this space is known to the given player.
    /// </summary>
    public bool IsKnownToPlayer(char player) 
        => KnownToPlayersSet.Contains(player);
    
    /// <summary>
    /// Mark this space as known to the given player.
    /// </summary>
    public void MakeKnownToPlayer(char player) {
        _knownToPlayersSet.Add(player);
    }

    /// <summary>
    /// Get the display value of this space for the given player.  Show always
    /// if the player is null.
    /// </summary>
    public string ToString(char? player)
        => (!player.HasValue || IsKnownToPlayer(player.Value))
            ? (MarkChar.ToString() ?? " ")
            : " ";
}

/// <summary>
/// Score for a single player
/// </summary>
public record struct PlayerScore(char Player, int Score);

/// <summary>
/// Read-only value-y collection of scores
/// </summary>
public record ScoreCard {
    #region constructors
    public ScoreCard() {
        Scores = Array.Empty<PlayerScore>();
    }
    public ScoreCard(char player, int score) : this(new PlayerScore(player, score)) {}
    public ScoreCard(PlayerScore playerScore) : this(new[]{playerScore}) {}
    public ScoreCard(IEnumerable<PlayerScore> scores) {
        Scores = scores
            .GroupBy(s => s.Player)
            .Select(g => new PlayerScore(g.Key, g.Sum(kvp => kvp.Score)))
            .ToArray();
    }

    #endregion

    private PlayerScore[] Scores {get;set;}

    public PlayerScore? HighestScore
        => Scores.Length == 0 
            ? null
            : Scores.MaxByStrict(s => s.Score);

    public static ScoreCard operator +(ScoreCard a, ScoreCard b)
        => new ScoreCard(a.Scores.Concat(b.Scores));
    public static ScoreCard operator +(ScoreCard a, PlayerScore b)
        => new ScoreCard(a.Scores.Append(b));
    public static ScoreCard operator +(PlayerScore a, ScoreCard b)
        => new ScoreCard(a) + b;
}

/// <summary>
/// Parameters to create a board.
/// </summary>
public record struct BoardBuilder(byte Width, byte Height);

/// <summary>
/// JSON-serializable model object for a single tic-tac-toe board.
/// </summary>
public record Board {
    #region constructors
    public Board() {}
    public Board(BoardBuilder builder) : this(builder.Width, builder.Height) {}
    public Board(byte width, byte height) {
        Spaces = new Space[width, height];
        for (var col = 0; col < Spaces.GetLength(0); col+=1) {
            for (var row = 0; row < Spaces.GetLength(1); row+=1) {
                Spaces[col,row] = new Space();
            }
        }
    }
    #endregion

    #region main data properties
    public Space[,] Spaces {get;set;} = new Space[1,1]{{new Space()}}; //default value is dummy board, never use.
    #endregion

    #region Methods
    /// <summary>
    /// For the given space on the board, generate the space's index code.
    /// </summary>
    public int GetSpaceIndexCode(int col, int row) {
        //aims for basic 3x3, but larger if needed
        //7 8 9
        //4 5 6
        //1 2 3
        return Spaces.GetLength(1) * (Spaces.GetLength(0) - 1) //top-left
            + col
            - row * Spaces.GetLength(0)
            + 1; //1-based
    }

    /// <summary>
    /// For the given space index code, find the coordinates.  Uses a "Try"
    /// signature so that it shall return false if the given spaceindex is not
    /// on the board at all.
    /// </summary>
    public bool TryGetCoordinatesFromSpaceIndexCode(int spaceIndex, out int resultCol, out int resultRow) {
        //brute-force search
        //todo: smarter algo
        for (var col = 0; col < Spaces.GetLength(0); col+=1) {
            for (var row = 0; row < Spaces.GetLength(1); row+=1) {
                if(GetSpaceIndexCode(col, row) == spaceIndex) {
                    resultCol = col;
                    resultRow = row;
                    return true;
                }
            }
        }
        resultCol = resultRow = -1;
        return false;
    }
        
    /// <summary>
    /// Get all of the spaces on the board as a big enumerable that you can
    /// foreach across.
    /// </summary>
    public IEnumerable<Space> BoardAsEnumerable() {
        foreach(var space in Spaces) {
            yield return space;   
        }
    }
    #endregion

    #region helper properties
    [JsonIgnore()]
    public int ColumnCount
        => Spaces.GetLength(0);

    [JsonIgnore()]    
    public int RowCount
        => Spaces.GetLength(1);
        
    /// <summary>
    /// Get how many spaces are on the board.
    /// </summary>
    [JsonIgnore()]
    public int SpaceCount
        => Spaces.GetLength(0) * Spaces.GetLength(1);

    /// <summary>
    /// Get how many digits the users will have to type in to type in a
    /// space-code.
    /// </summary>
    [JsonIgnore()]
    public int SpaceIndexCodeLength
        => SpaceCount.ToString().Length;

    /// <summary>
    /// Returns true if the game has ended in a tie.
    /// </summary>
    [JsonIgnore()]
    public bool IsFull 
        => BoardAsEnumerable().All(s => s.MarkChar.HasValue);

    [JsonIgnore()]
    public bool IsDone
        => IsFull 
        || ScoreCard.HighestScore.HasValue;

    [JsonIgnore()]
    public ScoreCard ScoreCard {
        get {
            var result = new List<PlayerScore>();
            //search for full-rows
            for(int row = 0; row < Spaces.GetLength(1); row+=1) {
                bool isWinner = true;
                var comparator = Spaces[0,row].MarkChar;
                if(comparator.HasValue) {
                    for(int col = 1; col < Spaces.GetLength(0); col+=1) {
                        if(comparator != Spaces[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        result.Add(new PlayerScore(comparator.Value, 1));
                    }
                }
            }
            //search for full-columns
            //todo: deduplicate.  Rotate array and re-run?
            for(int col = 0; col < Spaces.GetLength(0); col+=1) {
                bool isWinner = true;
                var comparator = Spaces[col,0].MarkChar;
                if(comparator.HasValue) {
                    for(int row = 1; row < Spaces.GetLength(1); row+=1) {
                        if(comparator != Spaces[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        result.Add(new PlayerScore(comparator.Value, 1));
                    }
                }
            }
            
            //create dummy scope to hide comparator var
            {
                //todo: support non-square Spacess, deduplicate.
                //identity diagonal
                var comparator = Spaces[0,0].MarkChar;
                if(comparator.HasValue) {
                    bool isWinner = true;
                    for(int col=1; col < Spaces.GetLength(0) && col < Spaces.GetLength(1); col+=1) {
                        var row = col;
                        if(comparator != Spaces[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        result.Add(new PlayerScore(comparator.Value, 1));
                    }
                }
            }
            //create dummy scope to hide comparator var
            {
                //inverse diagonal
                var comparator = Spaces[0,Spaces.GetLength(1)-1].MarkChar;
                if(comparator.HasValue) {
                    bool isWinner = true;
                    for(int col=1; col < Spaces.GetLength(0) && col < Spaces.GetLength(1); col+=1) {
                        var row = Spaces.GetLength(1) - 1 - col;
                        if(comparator != Spaces[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        result.Add(new PlayerScore(comparator.Value, 1));
                    }
                }
            }
            return new ScoreCard(result);
        }
    }
    #endregion
}

/// <summary>
/// Object that models the full state of the game.  Is serialized into a json file so all players can share reading it.
/// </summary>
public record TicTacToeState {
    #region constructors
    /// <summary>
    /// This constructor is only used by the serializer, never use it.
    /// </summary>
    public TicTacToeState(){}

    /// <summary>
    /// Construct a new gamestate object.  Note that there is no protection at this level against impossible values.
    /// </summary>
    public TicTacToeState(char[] players, bool isRandomPlayer, IEnumerable<BoardBuilder> boardBuilders) : this() {
        if(isRandomPlayer) {
            Random.Shared.Shuffle(players);
        }
        Players = players.ToList();
        Boards = boardBuilders.Select(b => new Board(b)).ToList();
    }
    #endregion

    #region main data properties
    public IReadOnlyList<char> Players {get;init;} = new List<char>();

    private int _currentTurnPlayerIndex = 0;
    /// <summary>
    /// index within list of *active* players - does not include resigned players.
    /// </summary>
    public int CurrentTurnPlayerIndex {
        get { return _currentTurnPlayerIndex; }
        set { 
            _currentTurnPlayerIndex = value; 
            RefreshCurrentPlayerTurnIndex();
        }
    }

    public HashSet<char> ResignedPlayersSet {get;init;} = new HashSet<char>();
    public IReadOnlyList<Board> Boards {get;init;}
    
    #endregion

    #region methods
    /// <summary>
    /// Advance to the next player's turn.  Do not call this if the current
    /// player has resigned.
    /// </summary>
    public void NextTurn() {
        CurrentTurnPlayerIndex += 1;
    }

    /// <summary>
    /// It's possible that the current player turn can exceed the number of
    /// players.  In that event, wrap around.
    /// </summary>
    public void RefreshCurrentPlayerTurnIndex()
        => _currentTurnPlayerIndex = CurrentTurnPlayerIndex % ActivePlayers.Count();
    
    /// <summary>
    /// Test if the given player has resigned.
    /// </summary>
    public bool IsResignedPlayer (char player) 
        => ResignedPlayersSet.Contains(player);

    /// <summary>
    /// Mark the given player as resigned.
    /// </summary>
    public void ResignPlayer (char player) {
        ResignedPlayersSet.Add(player);
        RefreshCurrentPlayerTurnIndex();
    }
    
    public Board GetBoardByCode(int boardCode)
        => Boards[boardCode-1];

    public OneOf<NotFound, BoardIsDone, Result<int>> SelectBoard(int boardCode)
        => (boardCode <= 0 || boardCode > Boards.Count)
            ? new NotFound()
            // doNextTurn = false;
            // Console.WriteLine ($"That is not a valid board.  Please pick an incomplete board from 1 to {state.Boards.Count}.");
            : GetBoardByCode(boardCode).IsDone
            ? new BoardIsDone()
            : new Result<int>(boardCode - 1);

    public OneOf<Success, Result<char>, AlreadyPlayed, NotFound> PlaySpace(int boardIndex, int spaceCode) {
        var board = Boards[boardIndex];
        if (board.TryGetCoordinatesFromSpaceIndexCode(spaceCode, out var col, out var row)) {
            return PlaySpace(boardIndex, col, row)
                .Match<OneOf<Success, Result<char>, AlreadyPlayed, NotFound>>( //have to provide return-type when going from OneOf to OneOf
                    success => success,
                    result => result,
                    alreadyPlayed => alreadyPlayed
                );
        } else {
            return new NotFound();
        }
    }
    
    public OneOf<Success, Result<char>, AlreadyPlayed> PlaySpace(int boardIndex, int col, int row) {
        var board = Boards[boardIndex];
        if (board.Spaces[col,row].MarkChar == CurrentTurnPlayer) {
            return new AlreadyPlayed();
        } else {
            board.Spaces[col,row].MakeKnownToPlayer(CurrentTurnPlayer);
            var foundMark = board.Spaces[col,row].MarkChar;
            if (foundMark.HasValue) {
                return new Result<char>(foundMark.Value);
            } else {
                board.Spaces[col,row].MarkChar = CurrentTurnPlayer;
                return new Success();
            }
        }
    }
    #endregion

    #region helper properties
    
    /// <summary>
    /// Returns a list of the active board indices. 0-based.
    /// </summary>
    [JsonIgnore()]
    public IEnumerable<int> ActiveBoardIndices {get {
        for(int i = 0; i < Boards.Count; i+=1) {
            if(!Boards[i].IsDone) {
                yield return i;
            }
        }
    }}
    
    /// <summary>
    /// Returns null if there are 0 or multiple active boards. Board Index if there's 1.
    /// </summary>
    [JsonIgnore()]
    public int? SingleActiveBoardIndex {get {
        var firstElements = ActiveBoardIndices.Take(2).ToArray();
        return (firstElements.Length == 1)
            ? firstElements.Single()
            : null;
    }}

    /// <summary>
    /// Get all of the current active players.  Order is consistent.
    /// </summary>
    [JsonIgnore()]
    public IEnumerable<char> ActivePlayers
        => Players.Except(ResignedPlayersSet);
        
    /// <summary>
    /// Get the mark-char of the current-turn player.
    /// </summary>
    [JsonIgnore()]
    public char CurrentTurnPlayer
        => ActivePlayers.ElementAt(CurrentTurnPlayerIndex);
    
    [JsonIgnore()]
    public ScoreCard ScoreCard 
        => Boards.Aggregate(new ScoreCard(), (prod, next) => prod + next.ScoreCard);
    
    [JsonIgnore()]
    /// <summary>
    /// Get the winner of the game.  Returns null if nobody has won yet or the
    /// game was a tie.  Note this is a heavy calculation and is not cached, but
    /// computers are fast.  TODO: Optimization.
    /// </summary>
    public char? Winner {
        get 
        {
            if(!IsGameOver) {
                return null;
            }

            if(ActivePlayers.Count() == 1) {
                return ActivePlayers.Single();
            }
            
            var highestScore = ScoreCard.HighestScore;
            if(highestScore.HasValue) {
                return highestScore.Value.Player;
            }
            
            //no winner found.
            return null;
        }
    }
    
    /// <summary>
    /// Returns true if the game has ended, whether by tie or by winner.
    /// </summary>
    [JsonIgnore()]
    public bool IsGameOver
        => Boards.All(b => b.IsDone)
        || ActivePlayers.Count() == 1;
    
    /// <summary>
    /// Provides a short text summary of the current game-state. Particularly useful when the game is over.
    /// </summary>
    [JsonIgnore()]
    public string GameStateText
        => Winner.HasValue
            ? $"Player '{Winner}' wins!"
            : IsGameOver 
            ? "Tie game."
            : $"Player '{CurrentTurnPlayer}' turn.";
    
    #endregion
}


/// <summary>
/// Empty result struct for OneOf, used when a player tries to play on a space they already played.
/// </summary>
public struct AlreadyPlayed;

/// <summary>
/// Empty result struct for OneOf, used when the player tries to select a board that is done.
/// </summary>
public struct BoardIsDone;