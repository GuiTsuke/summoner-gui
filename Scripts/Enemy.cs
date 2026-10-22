using Godot;

public partial class Enemy : PathFollow2D
{
    public const string GroupName = "enemies";
    [Signal] public delegate void ReachedChestEventHandler();
    [Signal] public delegate void DefeatedEventHandler();
    [Export] public EnemyDefinition Definition { get; set; } = null!;
    [Export] public float Speed { get; set; } = 100;
    [Export] public float MaxHealth { get; set; } = 45;
    [Export] public int Reward { get; set; } = 10;
    public float CurrentHealth { get; private set; }
    public bool IsBoss => Definition.IsBoss;
    public event System.Action? HitReceived;
    public bool IsTargetable => !_finished && CurrentHealth > 0 && IsInsideTree() && !IsQueuedForDeletion();
    private bool _finished;
    private bool _configured;
    private ProgressBar _healthBar = null!;
    private CharacterVisual _visual = null!;
    public bool IsBurning => _burnRemaining > 0 && IsTargetable;
    private double _burnRemaining, _burnUntilTick;
    private float _burnDamage, _burnInterval;
    private AnimatedSprite2D? _burnVisual;

    public void ApplyBurn(float damage, float duration, float tickInterval)
    {
        if (!IsTargetable || !float.IsFinite(damage) || !float.IsFinite(duration)
            || !float.IsFinite(tickInterval) || damage <= 0 || duration <= 0 || tickInterval <= 0) return;
        bool active = IsBurning;
        _burnDamage = active ? Mathf.Max(_burnDamage, damage) : damage;
        _burnRemaining = duration;
        if (!active) { _burnInterval = Mathf.Max(.05f, tickInterval); _burnUntilTick = _burnInterval; }
        if (_burnVisual == null)
        {
            _burnVisual = new AnimatedSprite2D { Name = "BurnVisual", SpriteFrames = GD.Load<SpriteFrames>("res://Resources/Animations/elemental-effects.tres"),
                Scale = Vector2.One * .10f, Offset = new Vector2(0, -96), Position = new Vector2(0, -8),
                TextureFilter = TextureFilterEnum.Nearest, ZIndex = 2 };
            AddChild(_burnVisual);
        }
        _burnVisual.Visible = true;
        _burnVisual.Play("burn");
    }

    public void AdvanceBurn(double delta)
    {
        if (!IsBurning || delta < 0) return;
        double remainingStep = System.Math.Min(delta, _burnRemaining);
        _burnRemaining -= remainingStep;
        _burnUntilTick -= remainingStep;
        while (_burnUntilTick <= .000001 && IsTargetable)
        {
            _burnUntilTick += _burnInterval;
            TakeDamage(_burnDamage);
        }
        if (_burnRemaining <= .000001) ClearBurn();
    }

    private void ClearBurn()
    {
        _burnRemaining = 0;
        if (_burnVisual != null) { _burnVisual.Visible = false; _burnVisual.Stop(); }
    }

    public void Configure(EnemyDefinition definition, float healthMultiplier = 1)
    {
        Definition = definition;
        MaxHealth = Mathf.Max(1, definition.MaxHealth * healthMultiplier);
        Speed = definition.Speed;
        Reward = definition.Reward;
        _configured = true;
    }

    public override void _Ready()
    {
        if (!_configured) Configure(Definition);
        CurrentHealth = Mathf.Max(1, MaxHealth);
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthBar.MaxValue = CurrentHealth;
        _healthBar.Value = CurrentHealth;
        _healthBar.Visible = !IsBoss;
        _visual = GetNode<CharacterVisual>("Visual");
        _visual.Position = new Vector2(0, IsBoss ? 22 : 14);
        _visual.Configure(Definition.Frames, Definition.VisualScale, "move_front");
        AddToGroup(GroupName);
    }

    public void TakeDamage(float damage)
    {
        if (!IsTargetable || damage <= 0 || !float.IsFinite(damage)) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        _healthBar.Value = CurrentHealth;
        HitReceived?.Invoke();
        if (CurrentHealth > 0) { _visual.Hit(); return; }
        _finished = true;
        ClearBurn();
        RemoveFromGroup(GroupName);
        SetProcess(false);
        _healthBar.Visible = false;
        _visual.Die();
        GD.Print($"Inimigo derrotado: {Definition.DisplayName}");
        // Threat/target resolution is immediate, independent of the death animation.
        EmitSignal(SignalName.Defeated);
    }

    public void Cancel()
    {
        _finished = true;
        ClearBurn();
        SetProcess(false);
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        AdvanceBurn(delta);
        if (_finished) return;
        var before = GlobalPosition;
        Progress += Mathf.Max(Speed, 0) * (float)delta;
        var direction = GlobalPosition - before;
        if (direction.LengthSquared() > 0.001f)
            _visual.Rest(MovementAnimation(direction), false);
        if (ProgressRatio < 1) return;
        _finished = true;
        ClearBurn();
        SetProcess(false);
        GD.Print($"Inimigo chegou ao baú: {Definition.DisplayName}");
        EmitSignal(SignalName.ReachedChest);
        QueueFree();
    }

    public static string MovementAnimation(Vector2 direction)
    {
        // Small curve noise stays in the dominant axis; there is no upward pose in the sheet.
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y) * 1.15f)
            return direction.X < 0 ? "move_left" : "move_right";
        return "move_front";
    }
}
