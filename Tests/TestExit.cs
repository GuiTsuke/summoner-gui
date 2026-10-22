using Godot;

public static class TestExit
{
    public static async void Quit(Node owner, int code = 0)
    {
        var audio = owner.GetNodeOrNull<AudioController>("/root/GameAudio");
        if (audio != null) await audio.QuitApplication(code);
        else owner.GetTree().Quit(code);
    }
}
