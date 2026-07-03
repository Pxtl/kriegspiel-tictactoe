namespace MnkeyFog.Model.PlayerAIs;

[ModelSerializable]
public class CarlaAI : MonteCarloAI {
    public override string Description => "Carla, difficulty 6";
    public override int MaxDepth => 4;
}