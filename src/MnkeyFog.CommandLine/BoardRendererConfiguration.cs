using Newtonsoft.Json;

namespace MnkeyFog.CommandLine;

public record class BoardRendererConfiguration(
    int CellWidth,
    char? CellColumnSeparator, char? CellRowSeparator,
    string? TopLeftCorner, string? TopIntersection, string? TopRightCorner,
    string? LeftIntersection, string? MiddleIntersection, string? RightIntersection,
    string? BottomLeftCorner, string? BottomIntersection, string? BottomRightCorner,
    string EmptyBoardLeader, string DoneBoardLeader, Func<string, string> BoardNameMappingToBoardLeader
) {
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

    public static BoardRendererConfiguration FullPipes { get; } = new BoardRendererConfiguration(
        3, '│', '─',
        "┌", "┬", "┐",
        "├", "┼", "┤",
        "└", "┴", "┘",
        //leaders
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );

    public static BoardRendererConfiguration Borderless { get; } = new BoardRendererConfiguration(
        3, null, null,
        null, null, null,
        null, null, null,
        null, null, null,
        //leaders
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );

    public static BoardRendererConfiguration HashPipes { get; } = new BoardRendererConfiguration(
        3, '│', '─',
        null, null, null,
        null, "┼", null,
        null, null, null,
        //leaders
        "  ", " ✓", boardName => boardName.PadLeft(2)
    );
}