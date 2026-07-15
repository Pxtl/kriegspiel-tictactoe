using Microsoft.VisualBasic;

namespace MnkeyFog.Model.Tests;

public class ScoreCardTests {
    private int _playerXIndex = 0;
    private int _playerOIndex = 1;
    [Fact]
    public void Constructor_Empty_ReturnsEmptyScores() {
        var scoreCard = new ScoreCard();
        scoreCard.Highest.Should().Be(ScoreCard.Empty);
    }

    [Fact]
    public void Constructor_SingleScore() {
        var scoreCard = new ScoreCard(_playerXIndex, 5);
        scoreCard.Highest.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_MultipleScores() {
        var scoreCard = new ScoreCard(new[] {
            new PlayerIndexScore(_playerXIndex, 3),
            new PlayerIndexScore(_playerOIndex, 2)
        });

        scoreCard.Highest.Should().NotBeNull();
    }

    [Fact]
    public void OperatorPlus() {
        var a = new ScoreCard(_playerXIndex, 3);
        var b = new ScoreCard(_playerOIndex, 2);

        var result = a + b;
        result.Highest.Should().NotBeNull();
    }
}
