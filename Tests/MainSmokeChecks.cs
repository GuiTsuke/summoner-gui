using Godot;

public partial class MainSmokeChecks : Node
{
    private int _frames;
    public override void _Ready() => AddChild(GD.Load<PackedScene>("res://Main.tscn").Instantiate());
    public override void _Process(double delta)
    {
        if (++_frames != 120) return;
        SetProcess(false);
        GD.Print("MainSmokeChecks: PASS");
        GetNode<AudioController>("/root/GameAudio").Notification((int)NotificationWMCloseRequest);
    }
}
