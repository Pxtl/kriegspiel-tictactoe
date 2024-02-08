#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1"
#r "nuget: Newtonsoft.Json, 13.0.3"

// Copyright (c) 2024 Martin Zarate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Dynamic;
using Newtonsoft.Json;
using System.CommandLine;
using System.Linq;
using System.Threading;

//class to represent a space on the board.
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
/// Default file location for the game state.  You will have to delete this file manually if it should become corrupted.
/// </summary>
public static FileInfo DefaultFilePath 
    => new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KriegsspielTicTacToe.json"));

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
    public TicTacToeState(char[] players, bool isRandomPlayer, byte width, byte height) : this() {
        if(isRandomPlayer) {
            Random.Shared.Shuffle(players);
        }
        Players = players.ToList();
        Board = new Space[width, height];
        for (var col = 0; col < Board.GetLength(0); col+=1) {
            for (var row = 0; row < Board.GetLength(1); row+=1) {
                Board[col,row] = new Space();
            }
        }
    }
    #endregion

    #region main data properties
    public IReadOnlyList<char> Players {get;} = new List<char>();

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

    public HashSet<char> ResignedPlayersSet {get;set;} = new HashSet<char>();
    
    public Space[,] Board {get;set;} = new Space[1,1]{new Space()};
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
    /// Get all of the spaces on the board as a big enumerable that you can
    /// foreach across.
    /// </summary>
    public IEnumerable<Space> BoardAsEnumerable() {
        foreach(var space in Board) {
            yield return space;   
        }
    }
    
    /// <summary>
    /// Test if the given player has resigned.
    /// </summary>
    public bool IsResignedPlayer(char player) 
        => ResignedPlayersSet.Contains(player);

    /// <summary>
    /// Mark the given player as resigned.
    /// </summary>
    public void ResignPlayer(char player) {
        ResignedPlayersSet.Add(player);
        RefreshCurrentPlayerTurnIndex();
    }
    
    /// <summary>
    /// Get all of the current active players.  Order is consistent.
    /// </summary>
    public IEnumerable<char> ActivePlayers
        => Players.Except(ResignedPlayersSet);
        
    /// <summary>
    /// Get the mark-char of the current-turn player.
    /// </summary>
    public char CurrentTurnPlayer
        => ActivePlayers.ElementAt(CurrentTurnPlayerIndex);
    
    /// <summary>
    /// Get how many spaces are on the board.
    /// </summary>
    public int SpaceCount
        => Board.GetLength(0) * Board.GetLength(1);
    
    /// <summary>
    /// Get how many digits the users will have to type in to type in a
    /// space-code.
    /// </summary>
    public int SpaceIndexCodeLength
        => (SpaceCount-1).ToString().Length;

    /// <summary>
    /// For the given space, for the given player, get the textual
    /// representation of the space.  If the game is over, all players see
    /// everything, all marks on the board.  If not, they will only see the ones
    /// they have created or discovered.  If the player is the
    /// current-turn-player, then the space index codes will be displayed.
    /// </summary>
    public string GetSpaceString(char? player, int col, int row) {
        player = IsGameOver //show for all players if the game is over.
            ? (char?)null
            : player;

        var foundSpace = Board[col,row].ToString(player);
        
        if(string.IsNullOrWhiteSpace(foundSpace) && player == CurrentTurnPlayer) {
            return GetSpaceIndexCode(col, row)
                .ToString(new string('0', SpaceIndexCodeLength)); //zero-pad
        } else {
            return foundSpace;
        }
    }

    /// <summary>
    /// For the given space on the board, generate the space's index code.
    /// </summary>
    public int GetSpaceIndexCode(int col, int row) {
        //aims for basic 3x3, but larger if needed
        //7 8 9
        //4 5 6
        //1 2 3
        return Board.GetLength(1) * (Board.GetLength(0) - 1) //top-left
            + col
            - row * Board.GetLength(0)
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
        for (var col = 0; col < Board.GetLength(0); col+=1) {
            for (var row = 0; row < Board.GetLength(1); row+=1) {
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
    /// Returns true if the game has ended in a tie.
    /// </summary>
    public bool IsTie =>
        BoardAsEnumerable().All(s => s.MarkChar.HasValue);
    
    /// <summary>
    /// Get the winner of the game.  Returns null if nobody has won yet or the
    /// game was a tie.  Note this is a heavy calculation and is not cached, but
    /// computers are fast.  TODO: Optimization.
    /// </summary>
    public char? Winner {
        get 
        {
            if(ActivePlayers.Count() == 1) {
                return ActivePlayers.Single();
            }
            //search for full-rows
            for(int row = 0; row < Board.GetLength(1); row+=1) {
                bool isWinner = true;
                var comparator = Board[0,row].MarkChar;
                if(comparator.HasValue) {
                    for(int col = 1; col < Board.GetLength(0); col+=1) {
                        if(comparator != Board[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        return comparator.Value;
                    }
                }
            }
            //search for full-columns
            //todo: deduplicate.  Rotate array and re-run?
            for(int col = 0; col < Board.GetLength(0); col+=1) {
                bool isWinner = true;
                var comparator = Board[col,0].MarkChar;
                if(comparator.HasValue) {
                    for(int row = 1; row < Board.GetLength(1); row+=1) {
                        if(comparator != Board[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        return comparator.Value;
                    }
                }
            }
            
            //create dummy scope to hide comparator var
            {
                //todo: support non-square boards, deduplicate.
                //identity diagonal
                var comparator = Board[0,0].MarkChar;
                if(comparator.HasValue) {
                    bool isWinner = true;
                    for(int col=1; col < Board.GetLength(0) && col < Board.GetLength(1); col+=1) {
                        var row = col;
                        if(comparator != Board[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        return comparator.Value;
                    }
                }
            }
            //create dummy scope to hide comparator var
            {
                //inverse diagonal
                var comparator = Board[0,Board.GetLength(1)-1].MarkChar;
                if(comparator.HasValue) {
                    bool isWinner = true;
                    for(int col=1; col < Board.GetLength(0) && col < Board.GetLength(1); col+=1) {
                        var row = Board.GetLength(1) - 1 - col;
                        if(comparator != Board[col,row].MarkChar) {
                            isWinner = false;
                            break;
                        }
                    }
                    if(isWinner) {
                        return comparator.Value;
                    }
                }
            }
            
            //no winner found.
            return null;
        }
    }
    
    /// <summary>
    /// Returns true if the game has ended, whether by tie or by winner.
    /// </summary>
    public bool IsGameOver
        => IsTie || Winner.HasValue;
    
    /// <summary>
    /// Provides a short text summary of the current game-state. Particularly useful when the game is over.
    /// </summary>
    public string GameStateText
        => Winner.HasValue
            ? $"Player '{Winner}' wins!"
            : IsTie 
            ? "Tie game."
            : $"Player '{CurrentTurnPlayer}' turn.";
    
    #endregion
}

/// <summary>
/// Helper function to draw a border row of the board.
/// </summary>
public static void DrawBorderRow(int columns, string startBarString, string midBarString, string endBarString, string spanString) 
{
    Console.Out.Write($"{startBarString}{spanString}");
    
    for(var col = 0; col < columns-1; col+=1) {
        Console.Out.Write($"{midBarString}{spanString}");
    }
    Console.Out.Write($"{endBarString}\n");
}

/// <summary>
/// Helper function to draw the body-spaces of the board.
/// </summary>
public static void DrawSpaceBody(string body)
{
    body = body.PadLeft(2);
    body = body.PadRight(3);
    Console.Out.Write($"│{body}");
}

/// <summary>
/// Draws the full board based on the given gamestate, from the perspective of
/// the given player.
/// </summary>
public void DrawBoard(TicTacToeState state, char? player) {
    //top row border
    DrawBorderRow(state.Board.GetLength(0), "┌", "┬", "┐", "───");
    
    for(var row = 0; row < state.Board.GetLength(1); row+=1) {
        if(row > 0) {
            //internal border
            DrawBorderRow(state.Board.GetLength(0), "├", "┼", "┤", "───");
        }
        for(var col = 0; col < state.Board.GetLength(0); col+=1) {
            DrawSpaceBody(state.GetSpaceString(player, col, row));
        }
        Console.Out.Write("│\n");
    }
    DrawBorderRow(state.Board.GetLength(1), "└", "┴", "┘", "───");
    
    if(state.ResignedPlayersSet.Count > 0) {
        foreach(var resignedPlayer in state.ResignedPlayersSet) {
            Console.Out.WriteLine($" - player '{resignedPlayer}' is resigned.");   
        }
    }
}

/// <summary>
/// Load TicTacToeState state object from json at the given file path.  No
/// error-checking exists, will throw if file is inaccessible or nonexistent or
/// corrupt.
/// </summary>
public TicTacToeState LoadState(string filePath) {
    using(var sr = new StreamReader(filePath)) {
        return JsonConvert.DeserializeObject<TicTacToeState>(sr.ReadToEnd())!;
    }
}

/// <summary>
/// Write the TicTacToe state as json at the given file path. Will attempt to
/// hold a mutually-exclusive lock while writing.  Will replace any file that
/// exists at the given path.
/// </summary>
public void SaveState(TicTacToeState state, string filePath) {
    using(var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
    using(var writer = new StreamWriter(fileStream)) {
        writer.Write(JsonConvert.SerializeObject(state));
    }
}

/// <summary>
/// Run the full board-game, including command-line GUI.  See command-line
/// options for information about parameters.
/// </summary>
public void RunGame(FileInfo sharedStateFilePath, bool doForceNewGame, char[] players, bool isRandomPlayer, byte boardSize, char? joinAsPlayer) {
    //load state
    TicTacToeState state = null;
    if(sharedStateFilePath.Exists && !doForceNewGame) {
        Console.Out.WriteLine($"Loaded saved game!");
        state = LoadState(sharedStateFilePath.FullName);
    } else {
        Console.Out.WriteLine($"Starting new game!");
        state = new TicTacToeState(players, isRandomPlayer, boardSize, boardSize);
        SaveState(state, sharedStateFilePath.FullName);
    }

    if(joinAsPlayer.HasValue) {
        Console.Out.WriteLine($"Joining game-file '{sharedStateFilePath.FullName}' as player '{joinAsPlayer.Value}'.");
    } else {
        Console.Out.WriteLine($"Running in hotseat mode.");
    }
    
    Console.Out.WriteLine($"Players are {string.Join(", ", state.Players)}.");
    
    bool isDone = false;
    var doNextTurn = true;
    while(!isDone) {
        if(doNextTurn) {
            if(joinAsPlayer.HasValue) {
                //file-based multiplayer mode
                if(!state.Players.Contains(joinAsPlayer.Value)) {
                    throw new ApplicationException($"Invalid player join, player {joinAsPlayer.Value} is not a player in this game.");
                }
                bool isDoneWaiting = false;
                Console.Out.Write("Waiting for your turn.");

                while(!isDoneWaiting) {
                    state = LoadState(sharedStateFilePath.FullName);
                    if(joinAsPlayer.Value == state.CurrentTurnPlayer || state.IsGameOver) {
                        isDoneWaiting = true;
                    } else {
                        Console.Out.Write(".");
                        Thread.Sleep(100);//ms
                    }
                }
                Console.Out.WriteLine();
                if(state.IsGameOver) {
                    isDone = true;
                } else {
                    Console.Out.WriteLine($"Player {joinAsPlayer.Value} it is your turn.");
                }
            } else {
                //hotseat mode.
                Console.Out.WriteLine($"Player {state.CurrentTurnPlayer} ready?  Press any key to continue...");
                Console.ReadKey(intercept: true);
                Console.WriteLine();
            }
        }
        var board = state.Board;
        var playerToRender = state.CurrentTurnPlayer; //cache this value in case they resign and we need to check who it is.
        
        if(!isDone) {
            DrawBoard(state, playerToRender);
            
            Console.Out.WriteLine("Press numeric key(s) to play a space, or 'r' to resign.");
            
            var sb = new StringBuilder();
            do {
                var key = Console.ReadKey();
                sb.Append(key.KeyChar);
            } while (sb.Length < state.SpaceIndexCodeLength && int.TryParse(sb.ToString(), out int _)); //stop when we've reached length or it's non-numeric.
            Console.Out.WriteLine();
            var commandString = sb.ToString();

            if(commandString.Equals("R", StringComparison.OrdinalIgnoreCase)) {
                doNextTurn = true;
                state.ResignPlayer(state.CurrentTurnPlayer);
            } else if(int.TryParse(commandString, out int keyInt)) {
                if(state.TryGetCoordinatesFromSpaceIndexCode(keyInt, out var col, out var row)) {
                    if(board[col,row].IsKnownToPlayer(state.CurrentTurnPlayer)) {
                        doNextTurn = false;
                        Console.Out.WriteLine($"Invalid square, that square is already known to player {state.CurrentTurnPlayer}");
                    } else {
                        doNextTurn = true;
                        board[col,row].MakeKnownToPlayer(state.CurrentTurnPlayer);
                        if(board[col,row].MarkChar.HasValue) {
                            Console.WriteLine($"You've just discovered that this space is already taken by player '{board[col,row].MarkChar}'.");
                        } else {
                            board[col,row].MarkChar = state.CurrentTurnPlayer;
                        }
                    }
                } else {
                    doNextTurn = false;
                    Console.WriteLine($"That is not a valid square on the board.  Please provide a number from 1 to {state.SpaceCount - 1}.");
                }
            } else {
                doNextTurn = false;
                Console.WriteLine($"That is not a valid command. Press numeric key(s) to play a space, or 'r' to resign.");
            }
        }
               
        if(doNextTurn) {
            //skip going to the next player if the current player resigned, they're already "current".
            if(!state.IsResignedPlayer(playerToRender)) {
                state.NextTurn();
            }

            //save state if we're in hotseat mode or it's our turn.
            if(!joinAsPlayer.HasValue || playerToRender == joinAsPlayer.Value) {
                SaveState(state, sharedStateFilePath.FullName);
            }

            //since actual player in state is no longer this player, they will mismatch and hide menu keys.
            DrawBoard(state, playerToRender);
            
            if(!state.IsGameOver) {
                if(!joinAsPlayer.HasValue) {
                    //hotseat-mode only behavior.
                    Console.Out.WriteLine($"Press any key to continue...");
                    Console.ReadKey(intercept: true);
                    Console.Clear();
                }
            } else {
                Console.Out.WriteLine(state.GameStateText);
                isDone = true;
            }
        }
    }
    
    Console.Out.WriteLine("Game over.");
    Thread.Sleep(1000); //wait 1 second so other players can get the news.
    sharedStateFilePath.Delete();
}

//exection starts here.

//skip the name of the script-runner itself and the name of the script file.
var args = Environment.GetCommandLineArgs().Skip(2).ToArray();

#region options
var stateFileOption = new Option<FileInfo>(
    aliases: new[]{"--file", "-f"},
    description: "Path to the json file where gamestate is stored.",
    getDefaultValue: () => DefaultFilePath
);

var forceNewGameOption = new Option<bool>(
    aliases: new[]{"--force", "-f"},
    description: "If true, it will force a new game instead of loading the game at the gamestate file.  Will replace gamestate file."
);

var playersOption = new Option<string[]>(
    aliases: new[]{"--players", "-p"},
    description: "Players mark characters.",
    isDefault: true,
    parseArgument: result => {
        if(result.Tokens.Count == 0) {
            //default
            return new string[]{"X", "O"};
        } else if (result.Tokens.Count == 1) {
            result.ErrorMessage = "There must be at least 2 mark characters.";
            return null;
        } else if (result.Tokens.Any(t => t.Value.Length == 0 && t.Value.Length > 1)) {
            //TODO: allow 3 chars for mark.
            result.ErrorMessage = "Player mark characters must be 1 chars long.";
            return null;
        } else if(result.Tokens.Select(t => t.Value).Distinct().Count() != result.Tokens.Count) {
            result.ErrorMessage = "Player mark characters must be distinct from each other.";
            return null;
        } else {
            return result.Tokens.Select(t => t.Value).ToArray();
        }
    }
) {
    AllowMultipleArgumentsPerToken = true
};

var randomOption = new Option<bool>(
    aliases: new[]{"--random", "-r"},
    description: "Randomize player order."
);

var sizeOption = new Option<byte?>(
    aliases: new[]{"--size", "-s"},
    description: "Board size.  Default is 3x3.",
    isDefault: true,
    parseArgument: result => {
        if(result.Tokens.Count == 0) {
            //default
            return 3;
        }

        if(byte.TryParse(result.Tokens.Single().Value, out byte size)) {
            if(2<=size && size <= 10) {
                return size;
            }
        }
        //did not pass above criteria.
        result.ErrorMessage = "Size must be a number from 2 to 10";
        return null;
    }
);

var joinAsPlayerOption = new Option<string>(
    aliases: new[]{"--join", "-j"},
    description: "Join as given player char mark. Must match a mark in players list. Hotseat mode if not provided."
);
#endregion

var rootCommand = new RootCommand("This is a simple command-line implementation of Zach Weinersmith's proposed game 'Kriegsspiel Tic Tac Toe'"){
    stateFileOption, forceNewGameOption, playersOption, randomOption, sizeOption, joinAsPlayerOption
};

rootCommand.SetHandler(
    (file, doForceNewGame, players, isRandomPlayer, size, joinAsPlayer) => {
        RunGame(
            file,
            doForceNewGame,
            players.Select(p => p.Single()).ToArray(), 
            isRandomPlayer,
            size.Value,
            (joinAsPlayer ?? "").Cast<char?>().SingleOrDefault()
        );
    }, 
    //the duplication of the option-list here is hideous but I don't know what to do about it. System.Commandline is ugly.
    stateFileOption, forceNewGameOption, playersOption, randomOption, sizeOption, joinAsPlayerOption
);

rootCommand.Invoke(args);