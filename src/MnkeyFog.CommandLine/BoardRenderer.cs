using System.Text;

namespace MnkeyFog.CommandLine;

/// <summary>
/// Draws the full board based on the given gamestate, from the perspective of
/// the given player.
/// </summary>
public class BoardRenderer {
    #region constructors
    public BoardRenderer()
    : this(BoardRendererConfiguration.FullPipes) {
    }

    public BoardRenderer(BoardRendererConfiguration configuration) {
        Configuration = configuration;
    }
    #endregion

    public BoardRendererConfiguration Configuration { get; }

    public string DrawBoards(
        GameView gameView,
        int maxRenderWidth = int.MaxValue
    ) {
        bool doShowBoardNameForAllBoards = gameView.BoardsCount > 1;
        var maxRowCount = gameView.Boards.Max(b => b.RowCount);
        var sb = new StringBuilder();

        sbyte nextDrawnBoardIndex = 0;
        var boardRenderWidth = GetBoardRenderWidth(gameView.GetBoardViewByIndex(nextDrawnBoardIndex));

        while (nextDrawnBoardIndex < gameView.BoardsCount) {
            sbyte drawBoardIndex = nextDrawnBoardIndex;
            var cellRowSeparatorChar = Configuration.CellRowSeparator ?? ' ';
            var cellColumnSeparatorChar = Configuration.CellColumnSeparator ?? ' ';
            var middleIntersection = (Configuration.HasColumnSeparators && Configuration.HasRowSeparators)
                ? Configuration.MiddleIntersection
                : Configuration.HasColumnSeparators
                ? cellColumnSeparatorChar.ToString()
                : Configuration.HasRowSeparators
                ? cellRowSeparatorChar.ToString()
                : "";

            var doShowBoardName = doShowBoardNameForAllBoards;
            if (Configuration.HasTopBorder) {
                DrawBorderRow(
                    gameView,
                    drawBoardIndex,
                    Configuration.HasLeftBorder ? Configuration.TopLeftCorner! : "",
                    Configuration.TopIntersection ?? cellRowSeparatorChar.ToString(),
                    Configuration.HasRightBorder ? Configuration.TopRightCorner! : "",
                    new string(cellRowSeparatorChar, Configuration.CellWidth),
                    doShowBoardName,
                    maxRenderWidth,
                    sb
                );
                doShowBoardName = false;
            }

            for (sbyte row = 0; row < maxRowCount; row += 1) {
                if (row > 0) {
                    if (Configuration.HasRowSeparators) {
                        DrawBorderRow(
                            gameView,
                            drawBoardIndex,
                            Configuration.HasLeftBorder ? Configuration.LeftIntersection! : "",
                            middleIntersection!,
                            Configuration.HasRightBorder ? Configuration.RightIntersection! : "",
                            new string(cellRowSeparatorChar, Configuration.CellWidth),
                            false,
                            maxRenderWidth,
                            sb
                        );
                    }
                }
                nextDrawnBoardIndex = DrawBoardSpacesRow(
                    gameView,
                    drawBoardIndex,
                    Configuration.HasLeftBorder,
                    Configuration.HasColumnSeparators,
                    Configuration.HasRightBorder,
                    cellColumnSeparatorChar.ToString(),
                    Configuration.CellWidth,
                    row,
                    boardRenderWidth,
                    doShowBoardName,
                    maxRenderWidth,
                    sb
                );
                doShowBoardName = false;
            }
            if (Configuration.HasBottomBorder) {
                DrawBorderRow(
                    gameView,
                    drawBoardIndex,
                    Configuration.HasLeftBorder ? Configuration.BottomLeftCorner! : "",
                    Configuration.BottomIntersection ?? cellRowSeparatorChar.ToString(),
                    Configuration.HasRightBorder ? Configuration.BottomRightCorner! : "",
                    new string(cellRowSeparatorChar, Configuration.CellWidth),
                    false,
                    maxRenderWidth,
                    sb
                );
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public int GetBoardRenderWidth(BoardView board)
        => board.ColumnCount * Configuration.CellWidth
        + (Configuration.HasColumnSeparators ? board.ColumnCount - 1 : 0)
        + (Configuration.HasLeftBorder ? 1 : 0)
        + (Configuration.HasRightBorder ? 1 : 0)
        + 2; //indent

    /// <summary>
    /// Helper function to draw a border row of the board.
    /// Wraps to newline when maxWidth is exceeded.
    /// </summary>
    private sbyte DrawBorderRow(
        GameView gameView,
        sbyte startBoardIndex,
        string startBarString,
        string midBarString,
        string endBarString,
        string spanString,
        bool doShowBoardName,
        int maxRenderWidth,
        StringBuilder sb
    ) {
        var boardIndex = startBoardIndex;
        for (; boardIndex < gameView.BoardsCount; boardIndex += 1) {
            var board = gameView.GetBoardViewByIndex(boardIndex);
            var cursorX = GetCursorX(sb);

            //wrap check - break if cursor would exceed maxWidth
            if (cursorX > 0 && (cursorX + GetBoardRenderWidth(board) > maxRenderWidth)) {
                break;
            }

            DrawBoardLeader(doShowBoardName, sb, boardIndex, board);

            sb.Append($"{startBarString}{spanString}");

            for (sbyte col = 0; col < board.ColumnCount - 1; col += 1) {
                sb.Append($"{midBarString}{spanString}");
            }
            sb.Append(endBarString);
        }
        sb.AppendLine();
        return boardIndex;
    }

    /// <summary>
    /// Draw a row of board spaces with window wrapping.
    /// Wraps to newline when maxWidth is exceeded.
    /// </summary>
    private sbyte DrawBoardSpacesRow(
        GameView gameView,
        sbyte startBoardIndex,
        bool hasLeftBorder,
        bool hasColumnSeparators,
        bool hasRightBorder,
        string columnSeparator,
        int cellWidth,
        sbyte rowIndex,
        int boardRenderWidth,
        bool doShowBoardName,
        int maxRenderWidth,
        StringBuilder sb
    ) {
        var boardIndex = startBoardIndex;
        for (; boardIndex < gameView.BoardsCount; boardIndex += 1) {
            var board = gameView.GetBoardViewByIndex(boardIndex);
            var cursorX = GetCursorX(sb);

            //wrap check - break if cursor would exceed maxWidth
            if (cursorX > 0 && (cursorX + boardRenderWidth > maxRenderWidth)) {
                break;
            }

            DrawBoardLeader(doShowBoardName, sb, boardIndex, board);

            if (hasLeftBorder) {
                sb.Append(columnSeparator);
            }
            for (sbyte col = 0; col < board.ColumnCount; col += 1) {
                if (col > 0 && hasColumnSeparators) {
                    sb.Append(columnSeparator);
                }
                var body = CommandNameTool.SpaceCommandName(gameView, boardIndex, col, rowIndex);
                DrawSpaceBody(body, cellWidth, sb);
            }
            if (hasRightBorder) {
                sb.Append(columnSeparator);
            }
        }
        sb.AppendLine();
        return boardIndex;
    }

    private void DrawBoardLeader(bool doShowBoardName, StringBuilder sb, sbyte boardIndex, BoardView board) {
        sb.Append(doShowBoardName
            ? (board.IsDone
                ? Configuration.DoneBoardLeader
                : Configuration.BoardNameMappingToBoardLeader(CommandNameTool.BoardNameFromIndex(boardIndex))
            ) : Configuration.EmptyBoardLeader
        );
    }

    /// <summary>
    /// Helper function to draw the body-spaces of the board.
    /// </summary>
    private static void DrawSpaceBody(string body, int cellWidth, StringBuilder sb) {
        body = body.PadLeft(cellWidth / 2 + 1);
        body = body.PadRight(cellWidth);
        sb.Append(body);
    }

    /// <summary>
    /// Get the cursor position (number of characters since last line break).
    /// </summary>
    public static int GetCursorX(StringBuilder sb) {
        int charsSinceLineBreak = 0;

        for (int i = sb.Length - 1; i >= 0; i--) {
            if (sb[i] == '\n') {
                break;
            }
            charsSinceLineBreak++;
        }
        return charsSinceLineBreak;
    }
}
