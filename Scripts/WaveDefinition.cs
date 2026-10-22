using Godot;

[GlobalClass]
public partial class WaveDefinition : Resource
{
    [Export] public Godot.Collections.Array<EnemyDefinition> Composition { get; set; } = new();
    [Export] public float HealthMultiplier { get; set; } = 1;
    [Export] public float SpawnInterval { get; set; } = 1.4f;
    public int EnemyCount => Composition.Count;
}
