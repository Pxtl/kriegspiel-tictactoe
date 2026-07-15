using MnkeyFog.Model.Template;
using MnkeyFog.Model.MNKGame;

namespace MnkeyFog.Model;

public static partial class GameTemplates {
    public static GameTemplate KriegspielTicTacToe { get; } = new MNKTemplate(
        "kriegspiel-tictactoe",
        "Zach Weinersmith's Kriegspiel Tic-Tac-Toe.",
        [2], //legal player-counts
        [
            new BoardBuilder(3, 3, new MNKBoardRuleset(IsBoardDoneWhenScored: true)),
            new BoardBuilder(3, 3, new MNKBoardRuleset(IsBoardDoneWhenScored: true)),
            new BoardBuilder(3, 3, new MNKBoardRuleset(IsBoardDoneWhenScored: true))
        ],
        isKriegspiel: true,
        isSynchronousMode: false
    );
}
