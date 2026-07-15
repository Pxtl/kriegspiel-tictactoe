using MnkeyFog.Model.Template;
using MnkeyFog.Model.Views;

namespace MnkeyFog.Model.PlayerAIs;

public interface IPlayerAI {
    //TODO: Remove ActionFactories from API, make convenient way to fetch it from GameView.
    void Attempt(GameView gameView);
    public string Description { get; }
}
