using Godot;

// One launch, one captured payload, at most one call to the shared damage flow.
public partial class ElementalAttack : Node2D
{
    public const string GroupName = "elemental_attacks";
    public const int EffectLimit = 48;
    public bool IsResolved { get; private set; }
    public float CapturedDamage => _attack.Damage;
    private AttackSnapshot _attack;
    private Creature _source = null!;
    private Enemy _target = null!;
    private Vector2 _origin, _secondary, _contact;
    private double _elapsed, _afterImpact;
    private float _extension;
    private SpriteFrames _frames = null!;
    private Sprite2D _sprite = null!;
    private Sprite2D? _secondJet;
    private readonly Vector2[] _arc = new Vector2[13];

    public void Configure(AttackSnapshot attack, Creature source, Enemy target, Vector2 origin, Vector2 secondary)
    {
        _attack = attack; _source = source; _target = target; _origin = origin; _secondary = secondary;
    }

    public override void _Ready()
    {
        AddToGroup(GroupName);
        ZIndex = 12;
        TextureFilter = TextureFilterEnum.Nearest;
        GlobalPosition = _origin;
        _frames = GD.Load<SpriteFrames>("res://Resources/Animations/elemental-effects.tres");
        _sprite = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest };
        AddChild(_sprite);
        if (_attack.Kind == ElementalAttackDefinition.Element.Water && _origin != _secondary)
        {
            _secondJet = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest, Centered = false, FlipH = true };
            AddChild(_secondJet);
        }
        UpdateVisual();
    }

    private bool ValidTarget() => GodotObject.IsInstanceValid(_target) && _target.IsTargetable;
    public void Cancel()
    {
        IsResolved = true;
        RemoveFromGroup(GroupName);
        SetProcess(false);
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (IsQueuedForDeletion()) return;
        if (IsResolved)
        {
            _afterImpact += delta;
            if (_afterImpact >= _attack.RetractDuration) { Cancel(); return; }
            UpdateVisual(); return;
        }
        if (!ValidTarget() || !GodotObject.IsInstanceValid(_source) || _source.IsQueuedForDeletion()) { Cancel(); return; }
        _elapsed += delta;
        if (_elapsed > _attack.Lifetime) { Cancel(); return; }
        _contact = _target.GlobalPosition + new Vector2(0, -8);
        if (_attack.Kind == ElementalAttackDefinition.Element.Fire)
        {
            GlobalPosition = GlobalPosition.MoveToward(_contact, _attack.Speed * (float)delta);
            Rotation = (_contact - GlobalPosition).Angle();
            if (GlobalPosition.DistanceSquaredTo(_contact) < .01f) Impact();
        }
        else if (_attack.Kind == ElementalAttackDefinition.Element.Water)
        {
            _extension += _attack.Speed * (float)delta;
            if (_extension >= _origin.DistanceTo(_contact)) Impact();
        }
        else if (_elapsed >= _attack.ContactDelay)
        {
            if (_source.GlobalPosition.DistanceSquaredTo(_target.GlobalPosition) <= _attack.Range * _attack.Range) Impact();
            else { Cancel(); return; }
        }
        UpdateVisual();
    }

    private void Impact()
    {
        if (IsResolved || !ValidTarget()) return;
        IsResolved = true;
        _target.TakeDamage(_attack.Damage);
        if (_attack.Kind == ElementalAttackDefinition.Element.Fire && ValidTarget() && _attack.BurnChance > 0
            && (_attack.BurnChance >= 1 || GD.Randf() < _attack.BurnChance))
            _target.ApplyBurn(_attack.BurnDamage, _attack.BurnDuration, _attack.BurnTickInterval);
    }

    private AtlasTexture Trimmed(string name, int frame)
    {
        var atlas = (AtlasTexture)_frames.GetFrameTexture(name, frame);
        // Drop virtual margins when fitting a jet/whip to a world-space span.
        return new AtlasTexture { Atlas = atlas.Atlas, Region = atlas.Region, FilterClip = true };
    }

    private void UpdateVisual()
    {
        int phase = (int)(_elapsed * 12) % 4;
        if (_attack.Kind == ElementalAttackDefinition.Element.Fire)
        {
            _sprite.Texture = _frames.GetFrameTexture("fireball", phase);
            _sprite.Offset = new Vector2(-192, 0);
            _sprite.Scale = Vector2.One * .12f;
            _sprite.Visible = !IsResolved;
        }
        else
        {
            string name = _attack.Kind == ElementalAttackDefinition.Element.Water ? "water_jet" : "vine_whips";
            // Four cached trimmed frames are created once, not on every process frame.
            _sprite.Texture = TrimmedFrame(name, phase);
            _sprite.Centered = false;
            _sprite.Offset = Vector2.Zero;
            Vector2 aim = _contact == Vector2.Zero && ValidTarget() ? _target.GlobalPosition - _origin : _contact - _origin;
            float length = aim.Length();
            float progress = _attack.Kind == ElementalAttackDefinition.Element.Water
                ? Mathf.Min(1, _extension / Mathf.Max(1, length)) : Mathf.Min(1, (float)(_elapsed / _attack.ContactDelay));
            if (IsResolved) progress = Mathf.Max(0, 1 - (float)(_afterImpact / _attack.RetractDuration));
            _sprite.Position = Vector2.Zero;
            _sprite.Rotation = aim.Angle();
            float height = _attack.Kind == ElementalAttackDefinition.Element.Water ? 20 : 28;
            _sprite.Scale = new Vector2(length * progress / _sprite.Texture.GetWidth(), height / _sprite.Texture.GetHeight());
            _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() / 2f);
            _sprite.FlipH = name == "water_jet";
            _sprite.Visible = progress > 0;
            if (_secondJet != null)
            {
                var secondAim = _contact - _secondary;
                _secondJet.Texture = _sprite.Texture;
                _secondJet.Position = ToLocal(_secondary);
                _secondJet.Rotation = secondAim.Angle();
                _secondJet.Offset = _sprite.Offset;
                _secondJet.Scale = new Vector2(secondAim.Length() * progress / _sprite.Texture.GetWidth(), _sprite.Scale.Y);
                _secondJet.Visible = _sprite.Visible;
            }
        }
        QueueRedraw();
    }

    private readonly Texture2D?[] _trimmed = new Texture2D?[4];
    private Texture2D TrimmedFrame(string name, int phase) => _trimmed[phase] ??= Trimmed(name, phase);

    public override void _Draw()
    {
        if (_attack.Kind != ElementalAttackDefinition.Element.Vines) return;
        float t = IsResolved ? Mathf.Max(0, 1 - (float)(_afterImpact / _attack.RetractDuration))
            : Mathf.Min(1, (float)(_elapsed / _attack.ContactDelay));
        for (int branch = 0; branch < 2; branch++)
        {
            var start = ToLocal(branch == 0 ? _origin : _secondary);
            var end = ToLocal(_contact);
            var normal = (end - start).Orthogonal().Normalized() * (branch == 0 ? 16 : -16);
            for (int i = 0; i < _arc.Length; i++)
            {
                float u = i / (float)(_arc.Length - 1) * t;
                _arc[i] = start.Lerp(end, u) + normal * Mathf.Sin(u * Mathf.Pi);
            }
            DrawPolyline(_arc, new Color(.10f, .24f, .04f), 6);
            DrawPolyline(_arc, new Color(.34f, .65f, .07f), 3);
        }
    }
}
