using Godot;

[Tool]
public partial class Creature : Node2D
{
    [Export] public PokemonDefinition Definition { get; set; } = null!;
    public string Lineage => Definition.Lineage;
    public int Level { get; private set; } = 1;
    public int FormIndex { get; private set; }
    public string DisplayName => Definition.FormNames[FormIndex];
    public float Damage => DamageAtLevel(Level);
    public float Range => Definition?.Range ?? 0;
    public float AttackInterval => Definition.AttackInterval;
    public float DamageAtLevel(int level) => Definition.CalculateDamage(level, FormIndex);
    public bool ShowRange { get => _showRange; set { _showRange = value; QueueRedraw(); } }

    private bool _showRange;
    private bool _combatEnabled = true;
    private double _cooldown;
    private CharacterVisual _visual = null!;
    private float _lastRange = -1;
    private float _evolutionEffect;

    public void Configure(PokemonDefinition definition)
    {
        Definition = definition;
        if (IsNodeReady()) UpdateVisual();
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        _visual = GetNode<CharacterVisual>("Visual");
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        _visual.Configure(Definition.Frames, Definition.VisualScale, $"idle_{FormIndex}");
        GetNode<Label>("LevelLabel").Text = $"Nv. {Level}";
    }

    public void Upgrade()
    {
        Level++;
        UpdateVisual();
        QueueRedraw();
    }

    public void Evolve()
    {
        if (FormIndex >= Definition.FormNames.Length - 1) return;
        FormIndex++;
        UpdateVisual();
        _evolutionEffect = 0.65f;
        QueueRedraw();
    }

    public void ResetTargetEffect()
    {
        _cooldown = 0;
        foreach (var child in GetChildren())
            if (child is ElementalAttack attack) attack.Cancel();
        _visual.ToRest();
    }

    public void SetCombatEnabled(bool enabled)
    {
        _combatEnabled = enabled;
        if (!enabled) ResetTargetEffect();
    }

    public override void _Draw()
    {
        if (Engine.IsEditorHint() || ShowRange)
            DrawArc(Vector2.Zero, Range, 0, Mathf.Tau, 96, new Color(0.1f, 0.42f, 0.64f, 0.6f), 2, true);
        if (_evolutionEffect > 0)
            DrawArc(Vector2.Zero, 35 + (0.65f - _evolutionEffect) * 20, 0, Mathf.Tau, 64,
                new Color(1, 0.85f, 0.2f, _evolutionEffect / 0.65f), 4, true);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (_lastRange != Range) { _lastRange = Range; QueueRedraw(); }
            return;
        }
        if (_evolutionEffect > 0)
        {
            _evolutionEffect = Mathf.Max(0, _evolutionEffect - (float)delta);
            QueueRedraw();
        }
        if (!_combatEnabled) return;
        _cooldown = System.Math.Max(0, _cooldown - delta);
        if (_cooldown > 0) return;

        var target = FindTarget();
        if (target == null) return;
        if (GetTree().GetNodesInGroup(ElementalAttack.GroupName).Count >= ElementalAttack.EffectLimit) return;
        bool left = target.GlobalPosition.X < GlobalPosition.X;
        var config = Definition.Attack;
        var snapshot = config.Capture(Level, Definition.FormMultipliers[FormIndex], Damage, Range);
        var attack = new ElementalAttack();
        attack.Configure(snapshot, this, target, ToGlobal(config.Emitter(FormIndex, left)),
            ToGlobal(config.Emitter(FormIndex, left, true)));
        AddChild(attack);
        _cooldown = Mathf.Max(AttackInterval, 0.05f);
        _visual.AttackPose($"idle_{FormIndex}", left);
    }

    private Enemy? FindTarget()
    {
        Enemy? target = null;
        float furthest = -1;
        foreach (var node in GetTree().GetNodesInGroup(Enemy.GroupName))
        {
            if (node is not Enemy enemy || !GodotObject.IsInstanceValid(enemy) || !enemy.IsTargetable) continue;
            if (GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition) > Range * Range) continue;
            if (enemy.Progress > furthest) { target = enemy; furthest = enemy.Progress; }
        }
        return target;
    }
}
