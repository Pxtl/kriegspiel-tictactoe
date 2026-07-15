namespace MnkeyFog.Model.PlayerAIs;

[ModelSerializable]
public class MontyAI : MonteCarloAI {
    public override string Description => "Monty, difficulty 5";
    public override int MaxDepth => 3;
}
