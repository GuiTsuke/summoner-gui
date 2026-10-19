using Godot;

public partial class Enemy : PathFollow2D
{
    [Signal]
    public delegate void ReachedChestEventHandler();

    [Export(PropertyHint.Range, "1,1000,1,or_greater,suffix:px/s")]
    public float Speed { get; set; } = 160.0f;

    public override void _Process(double delta)
    {
        // Progress is the distance along the parent Path2D, in pixels.
        Progress += Mathf.Max(Speed, 0.0f) * (float)delta;

        if (ProgressRatio >= 1.0f)
        {
            SetProcess(false);
            GD.Print("Inimigo chegou ao baú");
            EmitSignal(SignalName.ReachedChest);
            QueueFree();
        }
    }
}
