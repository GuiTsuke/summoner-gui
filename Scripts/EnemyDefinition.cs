using Godot;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [Export] public string Id { get; set; } = "slime";
    [Export] public string DisplayName { get; set; } = "Slime";
    [Export] public bool IsBoss { get; set; }
    [Export] public float MaxHealth { get; set; } = 45;
    [Export] public float Speed { get; set; } = 100;
    [Export] public int Reward { get; set; } = 10;
    [Export] public SpriteFrames Frames { get; set; } = null!;
    [Export] public float VisualScale { get; set; } = 0.16f;
}
