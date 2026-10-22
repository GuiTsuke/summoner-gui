using Godot;

// Presentation has no authority over attack timing, damage, target validity or waves.
public partial class CharacterVisual : AnimatedSprite2D
{
    private string _rest = "idle_0";
    private bool _dead;
    private Tween? _anticipation;
    public override void _Ready() => AnimationFinished += OnFinished;

    public void Configure(SpriteFrames frames, float scale, string rest)
    {
        ResetAnticipation();
        SpriteFrames = frames;
        Scale = Vector2.One * scale;
        _dead = false;
        _rest = rest;
        // All atlas textures share a padded canvas; the bottom is the foot anchor.
        Offset = new Vector2(0, -frames.GetFrameTexture(rest, 0).GetHeight() * 0.375f);
        Play(rest);
    }

    public void Rest(string animation, bool flip)
    {
        if (_dead) return;
        _rest = animation;
        FlipH = flip;
        if (!Animation.ToString().StartsWith("attack_") && Animation != "hit") Play(_rest);
    }

    public void Attack(string animation, bool flip)
    {
        if (_dead) return;
        FlipH = flip;
        Stop();
        Play(animation);
    }

    public void AttackPose(string idle, bool flip)
    {
        if (_dead) return;
        ResetAnticipation();
        FlipH = flip;
        Play(idle);
        // A small horizontal preparation preserves the foot height and uses no baked projectile pose.
        var anchor = Position;
        _anticipation = CreateTween();
        _anticipation.TweenProperty(this, "position", anchor + new Vector2(flip ? 2 : -2, 0), .06);
        _anticipation.TweenProperty(this, "position", anchor, .10);
    }

    private void ResetAnticipation()
    {
        if (_anticipation == null) return;
        _anticipation.Kill();
        _anticipation = null;
        Position = new Vector2(0, Position.Y);
    }

    public void Hit()
    {
        if (_dead) return;
        Stop();
        Play("hit");
    }

    public void Die()
    {
        _dead = true;
        Stop();
        Play("death");
    }

    public void ToRest()
    {
        if (_dead) return;
        ResetAnticipation();
        Stop();
        Play(_rest);
    }

    private void OnFinished()
    {
        if (_dead) GetParent().QueueFree();
        else Play(_rest);
    }
}
