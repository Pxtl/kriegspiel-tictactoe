using Newtonsoft.Json;

namespace MnkeyFog.CommandLine;

public class BoardRendererConfiguration {
    // Traditional constructor with nullable char types
    public BoardRendererConfiguration(
        int cellWidth,
        char? cellColumnSeparator, char? cellRowSeparator,
        string? topLeftCorner, string? topIntersection, string? topRightCorner,
        string? leftIntersection, string? middleIntersection, string? rightIntersection,
        string? bottomLeftCorner, string? bottomIntersection, string? bottomRightCorner,
        string emptyBoardLeader, string doneBoardLeader, Func<string, string> boardNameMappingToBoardLeader
    ) {
        CellWidth = cellWidth;
        CellColumnSeparator = cellColumnSeparator;
        CellRowSeparator = cellRowSeparator;
        TopLeftCorner = topLeftCorner;
        TopIntersection = topIntersection;
        TopRightCorner = topRightCorner;
        LeftIntersection = leftIntersection;
        MiddleIntersection = middleIntersection;
        RightIntersection = rightIntersection;
        BottomLeftCorner = bottomLeftCorner;
        BottomIntersection = bottomIntersection;
        BottomRightCorner = bottomRightCorner;
        EmptyBoardLeader = emptyBoardLeader;
        DoneBoardLeader = doneBoardLeader;
        BoardNameMappingToBoardLeader = boardNameMappingToBoardLeader;
    }

    // Properties for nullable char backing storage
    public int CellWidth { get; }
    public char? CellColumnSeparator { get; }
    public char? CellRowSeparator { get; }
    public string? TopLeftCorner { get; }
    public string? TopIntersection { get; }
    public string? TopRightCorner { get; }
    public string? LeftIntersection { get; }
    public string? MiddleIntersection { get; }
    public string? RightIntersection { get; }
    public string? BottomLeftCorner { get; }
    public string? BottomIntersection { get; }
    public string? BottomRightCorner { get; }
    public string EmptyBoardLeader { get; }
    public string DoneBoardLeader { get; }
    public Func<string, string> BoardNameMappingToBoardLeader { get; }

    // Extracted getter properties that return strings, replacing BoardRenderer.DrawBoards conversion logic
    [JsonIgnore]
    public string TopLeftCornerOrEmpty {
        get {
            if (HasLeftBorder) {
                return TopLeftCorner ?? "";
            }
            return "";
        }
    }

    [JsonIgnore]
    public string TopIntersectionOrRowSeparatorOrEmpty {
        get {
            if (TopIntersection != null) {
                return TopIntersection;
            }
            if (HasColumnSeparators) {
                return CellColumnSeparatorOrSpace.ToString();
            }
            if (HasRowSeparators) {
                return CellRowSeparatorOrSpace.ToString();
            }
            return "";
        }
    }

    [JsonIgnore]
    public string TopRightCornerOrEmpty {
        get {
            if (HasRightBorder) {
                return TopRightCorner ?? "";
            }
            return "";
        }
    }

    [JsonIgnore]
    public string LeftIntersectionOrEmpty {
        get {
            if (HasLeftBorder) {
                return LeftIntersection ?? "";
            }
            return "";
        }
    }

    [JsonIgnore]
    public string MiddleIntersectionOrColumnSeparatorOrRowSeparatorOrEmpty {
        get {
            if (MiddleIntersection != null) {
                return MiddleIntersection;
            }
            if (HasColumnSeparators) {
                return CellColumnSeparatorOrSpace.ToString();
            }
            if (HasRowSeparators) {
                return CellRowSeparatorOrSpace.ToString();
            }
            return "";
        }
    }

    [JsonIgnore]
    public string RightIntersectionOrEmpty {
        get {
            if (HasRightBorder) {
                return RightIntersection ?? "";
            }
            return "";
        }
    }

    [JsonIgnore]
    public string BottomLeftCornerOrEmpty {
        get {
            if (HasLeftBorder) {
                return BottomLeftCorner ?? "";
            }
            return "";
        }
    }

    [JsonIgnore]
    public string BottomIntersectionOrRowSeparatorOrEmpty {
        get {
            if (BottomIntersection != null) {
                return BottomIntersection;
            }
            if (HasColumnSeparators) {
                return CellColumnSeparatorOrSpace.ToString();
            }
            if (HasRowSeparators) {
                return CellRowSeparatorOrSpace.ToString();
            }
            return "";
        }
    }

    [JsonIgnore]
    public string BottomRightCornerOrEmpty {
        get {
            if (HasRightBorder) {
                return BottomRightCorner ?? "";
            }
            return "";
        }
    }

    // Helper properties for conversion
    [JsonIgnore]
    public char CellRowSeparatorOrSpace => CellRowSeparator ?? ' ';

    [JsonIgnore]
    public char CellColumnSeparatorOrSpace => CellColumnSeparator ?? ' ';

    [JsonIgnore]
    public string CellSeparatorSpanString => new string(CellRowSeparatorOrSpace, CellWidth);

    // Extracted properties for configuration analysis
    [JsonIgnore]
    public bool HasTopBorder => TopLeftCorner != null || TopRightCorner != null;

    [JsonIgnore]
    public bool HasBottomBorder => BottomLeftCorner != null || BottomRightCorner != null;

    [JsonIgnore]
    public bool HasLeftBorder => TopLeftCorner != null || BottomLeftCorner != null;

    [JsonIgnore]
    public bool HasRightBorder => TopRightCorner != null || BottomRightCorner != null;

    [JsonIgnore]
    public bool HasColumnSeparators => TopIntersection != null || MiddleIntersection != null || BottomIntersection != null;

    [JsonIgnore]
    public bool HasRowSeparators => LeftIntersection != null || MiddleIntersection != null || RightIntersection != null;

    // Factory methods for static configurations
    public static BoardRendererConfiguration FullPipes { get; } = new BoardRendererConfiguration(
        3, '│', '─',
        "┌", "┬", "┐",
        "├", "┼", "┤",
        "└", "┴", "┘",
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );

    public static BoardRendererConfiguration Borderless { get; } = new BoardRendererConfiguration(
        3, null, null,
        null, null, null,
        null, null, null,
        null, null, null,
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );

    public static BoardRendererConfiguration HashPipes { get; } = new BoardRendererConfiguration(
        3, '│', '─',
        null, null, null,
        null, "┼", null,
        null, null, null,
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );
}