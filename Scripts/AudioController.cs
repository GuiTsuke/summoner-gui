using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

// One autoload owns all playback. Gameplay never waits for a sound.
public partial class AudioController : Node
{
    [Export] public AudioSettings Settings { get; set; } = null!;
    public const int VoiceLimit = 6;
    public AudioStreamPlayer MusicPlayer { get; private set; } = null!;
    public AudioStreamPlayer ResultPlayer { get; private set; } = null!;
    public event Action<string>? SoundStarted;
    private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[VoiceLimit];
    private readonly Dictionary<string, AudioStreamOggVorbis> _sounds = new();
    private readonly Dictionary<string, double> _lastPlayed = new();
    private double _clock;
    private bool _result;
    private bool _closing;
    private Vector3 _applied = new(-1, -1, -1);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().AutoAcceptQuit = false;
        foreach (string name in new[] { "ui_click", "ui_select", "summon", "upgrade", "evolve", "coin",
            "denied", "wave_start", "boss_alert", "hit", "enemy_defeat", "victory", "defeat" })
        {
            // Private copies make loop settings independent of the import defaults.
            var stream = (AudioStreamOggVorbis)GD.Load<AudioStreamOggVorbis>($"res://Assets/Audio/sfx/{name}.ogg").Duplicate();
            stream.Loop = false;
            _sounds.Add(name, stream);
        }
        MusicPlayer = Player("MusicPlayer", "Music");
        var music = (AudioStreamOggVorbis)GD.Load<AudioStreamOggVorbis>("res://Assets/Audio/music/bards_tale_loop.ogg").Duplicate();
        music.Loop = true;
        MusicPlayer.Stream = music;
        ResultPlayer = Player("ResultPlayer", "SFX");
        for (int i = 0; i < VoiceLimit; i++) _voices[i] = Player($"Effect{i}", "SFX");
        ApplyVolumes();
        MusicPlayer.Play();
    }

    private AudioStreamPlayer Player(string name, string bus)
    {
        var player = new AudioStreamPlayer { Name = name, Bus = bus, MaxPolyphony = 1 };
        AddChild(player);
        return player;
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        var values = new Vector3(Settings.Master, Settings.Music, Settings.Sfx);
        if (values == _applied) return;
        SetBus("Master", values.X);
        SetBus("Music", values.Y);
        SetBus("SFX", values.Z);
        _applied = values;
    }

    private static void SetBus(string name, float value)
    {
        int bus = AudioServer.GetBusIndex(name);
        float volume = float.IsFinite(value) ? Mathf.Clamp(value, 0, 1) : 0;
        AudioServer.SetBusMute(bus, volume == 0);
        AudioServer.SetBusVolumeDb(bus, volume == 0 ? -80 : Mathf.LinearToDb(volume));
    }

    public void PlaySfx(string name)
    {
        if (_closing || _result || !_sounds.TryGetValue(name, out var stream)) return;
        double gap = name == "hit" ? 0.09 : name == "coin" || name == "enemy_defeat" ? 0.12 : 0.05;
        if (_lastPlayed.TryGetValue(name, out double last) && _clock - last < gap) return;
        foreach (var voice in _voices)
        {
            if (voice.Playing) continue;
            voice.Stream = stream;
            voice.VolumeDb = name == "hit" || name == "coin" ? -5 : 0;
            voice.Play();
            _lastPlayed[name] = _clock;
            SoundStarted?.Invoke(name);
            return;
        }
    }

    public void PlayResult(bool victory)
    {
        if (_closing || _result) return;
        _result = true;
        foreach (var voice in _voices) voice.Stop();
        MusicPlayer.VolumeDb = -10;
        string name = victory ? "victory" : "defeat";
        ResultPlayer.Stream = _sounds[name];
        ResultPlayer.Play();
        SoundStarted?.Invoke(name);
    }

    public void RestartPlayback()
    {
        if (_closing) return;
        _result = false;
        _lastPlayed.Clear();
        foreach (var voice in _voices) voice.Stop();
        ResultPlayer.Stop();
        MusicPlayer.VolumeDb = 0;
        if (!MusicPlayer.Playing) MusicPlayer.Play();
    }

    public void StopPlayback()
    {
        foreach (var node in GetChildren())
            if (node is AudioStreamPlayer player) { player.Stop(); player.Stream = null; }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) _ = QuitApplication();
    }

    public async Task QuitApplication(int code = 0)
    {
        if (_closing) return;
        _closing = true;
        StopPlayback();
        // Godot AudioServer releases Ogg playback asynchronously (engine issue #76745).
        // This real-time wait applies only to application shutdown, never gameplay/results.
        await Task.Delay(200);
        GetTree().Quit(code);
    }

    public override void _ExitTree()
    {
        StopPlayback();
        _sounds.Clear();
    }
}
