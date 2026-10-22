using Godot;

[GlobalClass]
public partial class AudioSettings : Resource
{
    [Export(PropertyHint.Range, "0,1,0.01")] public float Master { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Music { get; set; } = 0.22f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Sfx { get; set; } = 0.65f;
}
