using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

// Small private wave fixtures drive production actions/signals, not a second game implementation.
public partial class PresentationChecks : Node
{
    private Main _main = null!;
    private GameSession _session = null!;
    private AudioController _audio = null!;
    private readonly List<string> _startedSounds = new(); // Playback starts, not an assertion of human listening.
    private string _output = "";
    private bool _capture;

    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            int index = Array.IndexOf(args, "--report-dir");
            if (index >= 0) _output = args[index + 1];
            _capture = args.Contains("--capture");
            _main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
            AddChild(_main);
            _session = _main.GetNode<GameSession>("GameSession");
            _session.Director.SetProcess(false);
            _audio = GetNode<AudioController>("/root/GameAudio");
            _audio.SoundStarted += OnSound;
            await Frames(3);
            CheckResources();
            await Capture("presentation-initial");
            CheckVolumesAndLimits();
            await CheckPurchaseAudio();
            await CheckCombatAndResults();
            await CheckSceneReload();
            _audio.SoundStarted -= OnSound;
            GD.Print("PresentationChecks: PASS - assets, eventos, volumes, resultados e música única; áudio não foi ouvido por uma pessoa.");
            TestExit.Quit(this);
        }
        catch (Exception error) { GD.PushError(error.ToString()); TestExit.Quit(this, 1); }
    }

    private void OnSound(string name) => _startedSounds.Add(name);
    private void CheckResources()
    {
        var music = (AudioStreamOggVorbis)_audio.MusicPlayer.Stream;
        Require(music.Loop && music.GetLength() > 0 && _audio.MusicPlayer.Playing, "Música deve carregar e iniciar em loop.");
        foreach (string name in new[] { "slime", "goblin", "golem", "rei-ogro-boss" })
        {
            var frames = GD.Load<SpriteFrames>($"res://Resources/Animations/{name}.tres");
            foreach (string action in new[] { "move_right", "move_left", "move_front", "hit", "death" })
                Require(frames.HasAnimation(action) && frames.GetFrameCount(action) == 4, "Faltam poses direcionais de " + name);
        }
        Require(Enemy.MovementAnimation(Vector2.Right) == "move_right" && Enemy.MovementAnimation(Vector2.Left) == "move_left"
            && Enemy.MovementAnimation(Vector2.Down) == "move_front", "Direções devem selecionar a linha correta.");
        var curve = _main.GetNode<Path2D>("EnemyPath").Curve;
        var points = _main.GetNode<Line2D>("PathSurface").Points;
        Require(curve.PointCount == points.Length && points[0] == new Vector2(55, 176)
            && points[^1] == new Vector2(270, 632), "Trajeto original deve ser preservado.");
        for (int i = 0; i < points.Length; i++) Require(points[i] == curve.GetPointPosition(i), "Caminho visual deve acompanhar a rota.");
        for (int i = 1; i <= 9; i++)
        {
            var slot = Slot(i);
            var sprite = slot.GetNode<Sprite2D>("Base");
            var size = sprite.Texture.GetSize() * sprite.Scale;
            Require(size.X >= 90 && size.X < 120 && size.Y < 128, "Bases grandes precisam caber sem bloquear corredores.");
            Require(slot.GetNode<Button>("SelectButton").GetRect().HasPoint(Vector2.Zero), "Clique deve coincidir com a base.");
            Require(sprite.TextureFilter == CanvasItem.TextureFilterEnum.Nearest, "Base deve usar Nearest.");
        }
        CheckDependencies("res://Main.tscn", new HashSet<string>());
        CheckDependencies("res://Scenes/AudioController.tscn", new HashSet<string>());
    }

    private static void CheckDependencies(string path, HashSet<string> visited)
    {
        Require(!path.Contains("TempAssets", StringComparison.OrdinalIgnoreCase), "Recurso depende da pasta de entrada.");
        if (!visited.Add(path)) return;
        foreach (string entry in ResourceLoader.GetDependencies(path))
        {
            string dependency = entry.Split("::").Last();
            if (!dependency.StartsWith("res://")) continue;
            Require(ResourceLoader.Exists(dependency), "Referência quebrada: " + dependency);
            CheckDependencies(dependency, visited);
        }
    }

    private void CheckVolumesAndLimits()
    {
        var settings = _audio.Settings;
        var previous = new Vector3(settings.Master, settings.Music, settings.Sfx);
        settings.Master = 0; settings.Music = .4f; settings.Sfx = .3f; _audio.ApplyVolumes();
        Require(AudioServer.IsBusMute(AudioServer.GetBusIndex("Master")), "Zero deve silenciar Master.");
        Require(Mathf.IsEqualApprox(AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("Music")), .4f)
            && Mathf.IsEqualApprox(AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("SFX")), .3f), "Buses devem ter volumes independentes.");
        settings.Master = previous.X; settings.Music = previous.Y; settings.Sfx = previous.Z; _audio.ApplyVolumes();
        _audio.RestartPlayback(); _startedSounds.Clear();
        for (int i = 0; i < 30; i++) _audio.PlaySfx("hit");
        Require(_startedSounds.Count == 1, "Impactos consecutivos precisam ser limitados.");
        foreach (string name in new[] { "coin", "upgrade", "evolve", "summon", "denied", "boss_alert", "ui_click" }) _audio.PlaySfx(name);
        int voices = _audio.GetChildren().OfType<AudioStreamPlayer>().Count(p => p != _audio.MusicPlayer && p != _audio.ResultPlayer && p.Playing);
        Require(voices <= AudioController.VoiceLimit && _startedSounds.Count <= AudioController.VoiceLimit, "Limite de vozes excedido.");
    }

    private async Task CheckPurchaseAudio()
    {
        _session.StartingCoins = 500; _session.Restart();
        await ReadyCue(); _session.SelectSlot(Slot(2)); RequireCue("ui_select");
        await ReadyCue();
        _main.GetNode<Button>("HUD/Interface/Bottom/Rows/Portraits/Summon0").EmitSignal(BaseButton.SignalName.Pressed);
        Require(Slot(2).IsOccupied && _session.Coins == 450, "Compra real falhou."); RequireCue("ui_click"); RequireCue("summon");
        for (int i = 0; i < 3; i++)
        {
            await ReadyCue(); Require(_session.TryUpgrade(), "Melhoria real falhou.");
            RequireCue("upgrade");
            Require(!_startedSounds.Contains("evolve"), "Melhoria de nível não deve tocar evolução.");
        }
        Require(Slot(2).Occupant!.FormIndex == 0, "Nível quatro não pode evoluir automaticamente.");
        await ReadyCue(); Require(_session.TryEvolve(), "Compra de evolução real falhou."); RequireCue("evolve");
        Require(_session.Coins == 150 && Slot(2).Occupant!.FormIndex == 1 && Slot(2).Occupant!.Level == 4,
            "Evolução deve cobrar separadamente e preservar nível.");
        await Capture("presentation-evolution");
        _session.StartingCoins = 0; _session.Restart(); _session.SelectSlot(Slot(2));
        await ReadyCue(); Require(!_session.TrySummon("bulbasaur"), "Compra sem saldo deveria falhar."); RequireCue("denied");
        _session.StartingCoins = 150; _session.Restart();
    }

    private async Task CheckCombatAndResults()
    {
        var director = _session.Director;
        var ordinary = director.Waves[0].Composition[0];
        var boss = director.Waves.Last().Composition.Last();
        director.Waves = new Godot.Collections.Array<WaveDefinition>();
        for (int wave = 1; wave <= 10; wave++)
            director.Waves.Add(new WaveDefinition { Composition = new Godot.Collections.Array<EnemyDefinition> { wave == 10 ? boss : ordinary } });
        for (int wave = 1; wave <= 10; wave++)
        {
            await ReadyCue(); Require(_session.TryStartWave(), "Fixture deve iniciar onda."); RequireCue("wave_start");
            director._Process(0);
            var enemy = _main.GetNode<Path2D>("EnemyPath").GetChildren().OfType<Enemy>().Single(e => e.IsTargetable);
            enemy.SetProcess(false); enemy.Progress = 140;
            if (wave == 10) { RequireCue("boss_alert"); await Capture("presentation-boss"); }
            await ReadyCue(); enemy.TakeDamage(1); RequireCue("hit");
            await Capture(wave == 1 ? "presentation-combat" : "");
            await ReadyCue(); int before = _session.Coins; enemy.TakeDamage(float.MaxValue);
            Require(_session.Coins == before + enemy.Reward, "Som não deve interferir na recompensa.");
            RequireCue("coin");
            if (wave < 10) RequireCue("enemy_defeat");
            else RequireCue("victory");
        }
        Require(_session.State == GameSession.MatchState.Victory, "Fixture deveria vencer.");
        CheckJingle("victory"); await Capture("presentation-victory");
        _session.StartingLives = 1; _session.Restart();
        Require(!_audio.ResultPlayer.Playing && _audio.MusicPlayer.Playing, "Reinício deve parar vinheta e manter música.");
        await ReadyCue(); _session.TryStartWave(); director._Process(0);
        var arriving = _main.GetNode<Path2D>("EnemyPath").GetChildren().OfType<Enemy>().Single(e => e.IsTargetable);
        arriving.ProgressRatio = 1; arriving._Process(0);
        Require(_session.State == GameSession.MatchState.Defeat, "Chegada deveria causar derrota na fixture.");
        RequireCue("defeat"); CheckJingle("defeat"); await Capture("presentation-defeat");
        _session.Restart();
    }

    private void CheckJingle(string name)
    {
        Require(_audio.ResultPlayer.Playing && !((AudioStreamOggVorbis)_audio.ResultPlayer.Stream).Loop, "Vinheta deve tocar sem loop.");
        int before = _startedSounds.Count(c => c == name);
        _audio.PlayResult(name == "victory");
        Require(_startedSounds.Count(c => c == name) == before, "Resultado repetido não pode reiniciar vinheta.");
    }

    private async Task CheckSceneReload()
    {
        ulong identity = _audio.MusicPlayer.GetInstanceId();
        for (int i = 0; i < 3; i++) _session.Restart();
        _main.QueueFree(); await Frames(2);
        _main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>(); AddChild(_main);
        await Frames(2);
        Require(GetTree().Root.GetChildren().OfType<AudioController>().Count() == 1
            && _audio.MusicPlayer.GetInstanceId() == identity && _audio.MusicPlayer.Playing,
            "Reiniciar ou recriar a cena não deve duplicar/trocar o tocador de música.");
        GD.Print("PresentationChecks: 1 música persistente após três reinícios e recriação da Main; limite de 6 SFX.");
    }

    private CreatureSlot Slot(int index) => _main.GetNode<CreatureSlot>($"Slots/Slot{index}");
    private void RequireCue(string name) => Require(_startedSounds.Contains(name), "Evento não iniciou áudio: " + name);
    private async Task ReadyCue()
    {
        foreach (var player in _audio.GetChildren().OfType<AudioStreamPlayer>())
            if (player != _audio.MusicPlayer && player != _audio.ResultPlayer) player.Stop();
        await Frames(10); _startedSounds.Clear();
    }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string name)
    {
        if (!_capture || name == "") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Directory.CreateDirectory(_output);
        Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png")) == Error.Ok, "Captura falhou.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
