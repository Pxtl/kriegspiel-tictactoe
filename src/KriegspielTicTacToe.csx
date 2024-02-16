#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: OneOf, 3.0.263"
#load "Model.csx"

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

using System.CommandLine;
using System.Linq;
using System.Threading;
using System.IO;
using System.Dynamic;
using Newtonsoft.Json;
using OneOf;

/// <summary>
/// Helper function to draw a border row of the board.
/// </summary>
public static void DrawBorderRow(
    TicTacToeState state, 
    string startBarString, 
    string midBarString, 
    string endBarString, 
    string spanString, 
    bool showBoardCode)
{
    for (int boardIndex = 0; boardIndex < state.Boards.Count; boardIndex+=1) {
        var board = state.Boards[boardIndex];

        Console.Out.Write(showBoardCode
            ? (board.IsDone
                ? " ✓" //board is done so just show a checkmark.
                : $" {boardIndex + 1}" //key-index to choose it
            ): "  " //blank space
        );

        Console.Out.Write($"{startBarString}{spanString}");
        
        for(var col = 0; col < board.ColumnCount-1; col+=1) {
            Console.Out.Write($"{midBarString}{spanString}");
        }
        Console.Out.Write(endBarString);
    }
    Console.Out.WriteLine();
}

/// <summary>
/// Helper function to draw the body-spaces of the board.
/// </summary>
public static void DrawSpaceBody(string body, string borderBarString)
{
    body = body.PadLeft(2);
    body = body.PadRight(3);
    Console.Out.Write($"{borderBarString}{body}");
}

/// <summary>
/// For the given space, for the given player, get the textual
/// representation of the space.  If the game is over, all players see
/// everything, all marks on the board.  If not, they will only see the ones
/// they have created or discovered.  If the player is the
/// current-turn-player, then the space index codes will be displayed.
/// </summary>
public static string GetSpaceString(TicTacToeState state, char? player, int boardIndex, int? activeBoardIndex, int col, int row) {
    player = state.IsGameOver //show for all players if the game is over.
        ? (char?)null
        : player;

    var board = state.Boards[boardIndex];
    var foundSpace = board.Spaces[col,row].ToString(player);
    
    if (
        string.IsNullOrWhiteSpace(foundSpace) 
        && !board.IsDone 
        && player == state.CurrentTurnPlayer
        && activeBoardIndex == boardIndex
    ) {
        return board.GetSpaceIndexCode(col, row)
            .ToString(new string('0', board.SpaceIndexCodeLength)); //zero-pad
    } else {
        return foundSpace;
    }
}

public static void DrawBoardRow(TicTacToeState state, char? player, string borderBarString, int? activeBoardIndex, int rowIndex) {
    for (int boardIndex = 0; boardIndex < state.Boards.Count; boardIndex+=1) {
        var board = state.Boards[boardIndex];
        Console.Out.Write("  ");

        for(var col = 0; col < board.ColumnCount; col+=1) {
            DrawSpaceBody(GetSpaceString(state, player, boardIndex, activeBoardIndex, col, rowIndex), borderBarString);
        }
        Console.Out.Write(borderBarString);
    }
    Console.Out.WriteLine();
}

/// <summary>
/// Draws the full board based on the given gamestate, from the perspective of
/// the given player.
/// </summary>
public void DrawBoards(TicTacToeState state, char? player, int? activeBoardIndex) {
    bool doShowBoardCode = state.Boards.Count > 1;
    Board board = activeBoardIndex.HasValue 
        ? state.Boards[activeBoardIndex.Value]
        : null;
    var maxRowCount = state.Boards.Max(b => b.RowCount);

    //top row border
    DrawBorderRow(state, "┌", "┬", "┐", "───", doShowBoardCode);
    
    
    for(var row = 0; row < maxRowCount; row+=1) {
        if(row > 0) {
            //internal border
            DrawBorderRow(state, "├", "┼", "┤", "───", showBoardCode: false);
        }
        DrawBoardRow(state, player, "|", activeBoardIndex, row);
    }
    DrawBorderRow(state, "└", "┴", "┘", "───", showBoardCode: false);
    
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
public void RunGame(FileInfo sharedStateFilePath, bool doForceNewGame, char[] players, bool isRandomPlayer, IEnumerable<BoardBuilder> boardBuilders, char? joinAsPlayer) {
    //load state
    TicTacToeState state = null;
    if(sharedStateFilePath.Exists && !doForceNewGame) {
        Console.Out.WriteLine($"Loaded saved game!");
        state = LoadState(sharedStateFilePath.FullName);
    } else {
        Console.Out.WriteLine($"Starting new game!");
        state = new TicTacToeState(players, isRandomPlayer, boardBuilders);
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
        var playerToRender = state.CurrentTurnPlayer; //cache this value in case they resign and we need to check who it is.
        
        if (!isDone) {
            int? activeBoardIndex = null;
            if (state.Boards.Count > 1) {
                DrawBoards (state, playerToRender, activeBoardIndex);
                var boardCommand = ReadCommandKeys("Press numeric key(s) to pick a board, or 'r' to resign.", 1, new[]{'r'});
                boardCommand.Switch (
                    charCode => {
                        doNextTurn = true;
                        state.ResignPlayer(state.CurrentTurnPlayer);
                    },
                    keyInt => {
                        if(
                            0 < keyInt 
                            && keyInt <= state.Boards.Count
                            && !state.Boards[keyInt-1].IsDone
                        ) {
                            activeBoardIndex = keyInt - 1;
                        } else {
                            doNextTurn = false;
                            Console.WriteLine($"That is not a valid board.  Please pick an incomplete board from 1 to {state.Boards.Count}.");
                        }
                    },
                    unknown => {
                        doNextTurn = false;
                    }
                );
            } else {
                activeBoardIndex = 0;
            }

            if(activeBoardIndex.HasValue) {
                DrawBoards(state, playerToRender, activeBoardIndex);
                var board = state.Boards[activeBoardIndex.Value];
                var spaceCommand = ReadCommandKeys("Press numeric key(s) to play a space, or 'r' to resign.", board.SpaceIndexCodeLength, new[]{'r'});
                spaceCommand.Switch (
                    charCode => {
                        doNextTurn = true;
                        state.ResignPlayer(state.CurrentTurnPlayer);
                    },
                    keyInt => {
                        if (board.TryGetCoordinatesFromSpaceIndexCode(keyInt, out var col, out var row)) {
                            if (board.Spaces[col,row].IsKnownToPlayer(state.CurrentTurnPlayer)) {
                                doNextTurn = false;
                                Console.Out.WriteLine($"Invalid square, that square is already known to player {state.CurrentTurnPlayer}");
                            } else {
                                doNextTurn = true;
                                board.Spaces[col,row].MakeKnownToPlayer(state.CurrentTurnPlayer);
                                if (board.Spaces[col,row].MarkChar.HasValue) {
                                    Console.WriteLine($"You've just discovered that this space is already taken by player '{board.Spaces[col,row].MarkChar}'.");
                                } else {
                                    board.Spaces[col,row].MarkChar = state.CurrentTurnPlayer;
                                }
                            }
                        } else {
                            doNextTurn = false;
                            Console.WriteLine($"That is not a valid square on the board.  Please provide a number from 1 to {board.SpaceCount - 1}.");
                        }
                    },
                    unknown => {
                        doNextTurn = false;
                    }
                );
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
            DrawBoards(state, playerToRender, activeBoardIndex:null);
            
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

public static OneOf<char, int, OneOf.Types.Unknown> ReadCommandKeys(string prompt, int codeLength, char[] validCommandChars = null) {
    if(validCommandChars == null) {
        validCommandChars = new[]{'r'};
    }
    Console.Out.WriteLine(prompt);
    
    var sb = new StringBuilder();
    var numericCode = -1;
    var isNumeric = false;
    do {
        var key = Console.ReadKey();
        if(key.KeyChar >= '0' && key.KeyChar <= '9') {
            sb.Append(key.KeyChar);
        } else if(validCommandChars.Contains(key.KeyChar)){
            Console.Out.WriteLine();
            return key.KeyChar;
        } else {
            Console.Out.WriteLine();
            Console.Out.WriteLine("Invalid command.");
            return new OneOf.Types.Unknown();
        }
        isNumeric = int.TryParse(sb.ToString(), out numericCode);
    } while (sb.Length < codeLength && isNumeric); //stop when we've reached length or it's non-numeric.
    Console.Out.WriteLine();
    return numericCode;
}

/// <summary>
/// Default file location for the game state.  You will have to delete this file manually if it should become corrupted.
/// </summary>
public static FileInfo DefaultFilePath 
    => new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KriegspielTicTacToe.json"));

//### execution starts here. ###

#region options
var stateFileOption = new Option<FileInfo>(
    aliases: new[]{"--file", "-f"},
    description: "Path to the json file where gamestate is stored.  Will be resumed automatically if you kill the game (ctrl-C).  Use a fileshare for network multiplayer.",
    getDefaultValue: () => DefaultFilePath
);

var forceNewGameOption = new Option<bool>(
    aliases: new[]{"--force", "-F"},
    description: "Force a new game instead of loading the game at the gamestate file.  Will replace gamestate file."
);

var playersOption = new Option<string[]>(
    aliases: new[]{"--players", "-p"},
    description: "Players mark characters.  Provide them space-separated, eg '-p A B C X Y Z' for a 6-player game.",
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
        } else if(result.Tokens.Any(t => short.TryParse(t.Value, out short _))) {
            result.ErrorMessage = "Player mark characters must not be numeric.";
            return null;
        } else {
            return result.Tokens.Select(t => t.Value).ToArray();
        }
    }
) {
    AllowMultipleArgumentsPerToken = true //needed to support array-format -p A B C
};

var randomOption = new Option<bool>(
    aliases: new[]{"--random", "-r"},
    description: "Randomize player order."
);

var sizeOption = new Option<byte?>(
    aliases: new[]{"--size", "-z"}, //-s is taken by dotnet-script
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
var boardsNumberOption = new Option<byte?>(
    aliases: new[]{"--boards", "-b"},
    description: "Number of boards.",
    isDefault: true,
    parseArgument: result => {
        if(result.Tokens.Count == 0) {
            //default
            return 3;
        }

        if(byte.TryParse(result.Tokens.Single().Value, out byte size)) {
            if(2<=size && size <= 9) {
                return size;
            }
        }
        //did not pass above criteria.
        result.ErrorMessage = "Boards must be a number from 1 to 9";
        return null;
    }
);

var joinAsPlayerOption = new Option<string>(
    aliases: new[]{"--join", "-j"},
    description: "Join as given player char mark. Must match a mark in players list. Hotseat mode if not provided."
);
#endregion

var rootCommand = new RootCommand("This is a simple command-line implementation of Zach Weinersmith's proposed game 'Kriegspiel Tic Tac Toe'"){
    stateFileOption, forceNewGameOption, playersOption, randomOption, sizeOption, boardsNumberOption, joinAsPlayerOption
};

rootCommand.SetHandler(
    (file, doForceNewGame, players, isRandomPlayer, size, boardsNumber, joinAsPlayer) => {
        var boardBuilders = new BoardBuilder[boardsNumber.Value];
        for(var i = 0; i < boardsNumber.Value; i+=1) {
            boardBuilders[i] = new BoardBuilder(size.Value, size.Value);
        }
        RunGame (
            file,
            doForceNewGame,
            players.Select(p => p.Single()).ToArray(), 
            isRandomPlayer,
            boardBuilders,
            (joinAsPlayer ?? "").Cast<char?>().SingleOrDefault()
        );
    }, 
    //the duplication of the option-list here is hideous but I don't know what to do about it. System.Commandline is ugly.
    stateFileOption, forceNewGameOption, playersOption, randomOption, sizeOption, boardsNumberOption, joinAsPlayerOption
);

rootCommand.Invoke(Args.ToArray());