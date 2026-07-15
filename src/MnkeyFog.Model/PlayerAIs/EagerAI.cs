namespace MnkeyFog.Model.PlayerAIs;

[ModelSerializable]
public class EagerAI : MonteCarloAI {
    public override string Description => "Eager, difficulty 4";
    public override int MaxDepth => 1;
}
