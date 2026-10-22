using Godot;

[Tool, GlobalClass]
public partial class ElementalAttackDefinition : Resource
{
    public enum Element { Fire, Vines, Water }
    [Export] public Element Kind { get; set; }
    [Export] public float Speed { get; set; } = 300;
    [Export] public float ContactDelay { get; set; } = 0.18f;
    [Export] public float RetractDuration { get; set; } = 0.16f;
    [Export] public float Lifetime { get; set; } = 3;
    [Export] public float BurnBaseChance { get; set; } = 0.10f;
    [Export] public float BurnChancePerLevel { get; set; } = 0.02f;
    [Export] public float BurnChanceCap { get; set; } = 0.60f;
    [Export] public float BurnBaseDamage { get; set; } = 1;
    [Export] public float BurnDamagePerLevel { get; set; } = 0.5f;
    [Export] public float BurnDuration { get; set; } = 3;
    [Export] public float BurnTickInterval { get; set; } = 1;
    // Local scene pixels relative to the base center, for the preserved right-facing idle pose.
    // Left-facing offsets are editable independently, not assumed to be new supplied poses.
    [Export] public Vector2[] EmittersRight { get; set; } = { new(15, -20), new(17, -25), new(20, -28) };
    [Export] public Vector2[] EmittersLeft { get; set; } = { new(-15, -20), new(-17, -25), new(-20, -28) };
    [Export] public Vector2[] SecondaryRight { get; set; } = { new(-12, -22), new(-16, -25), new(-20, -28) };
    [Export] public Vector2[] SecondaryLeft { get; set; } = { new(12, -22), new(16, -25), new(20, -28) };

    public Vector2 Emitter(int form, bool left, bool secondary = false)
    {
        var positions = secondary ? (left ? SecondaryLeft : SecondaryRight) : (left ? EmittersLeft : EmittersRight);
        return positions[Mathf.Clamp(form, 0, positions.Length - 1)];
    }

    public AttackSnapshot Capture(int level, float multiplier, float damage, float range) => new(
        Kind, damage, range, Mathf.Max(1, Speed), Mathf.Max(.01f, ContactDelay),
        Mathf.Max(.01f, RetractDuration), Mathf.Max(.1f, Lifetime),
        Mathf.Clamp(BurnBaseChance + BurnChancePerLevel * (level - 1), 0, Mathf.Clamp(BurnChanceCap, 0, 1)),
        Mathf.Max(0, (BurnBaseDamage + BurnDamagePerLevel * (level - 1)) * multiplier),
        Mathf.Max(0, BurnDuration), Mathf.Max(.05f, BurnTickInterval));
}

public readonly record struct AttackSnapshot(ElementalAttackDefinition.Element Kind, float Damage, float Range,
    float Speed, float ContactDelay, float RetractDuration, float Lifetime, float BurnChance,
    float BurnDamage, float BurnDuration, float BurnTickInterval);
