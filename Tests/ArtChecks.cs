using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class ArtChecks : Node2D
{
    private readonly List<CharacterVisual> _pokemon = new();
    private readonly List<CharacterVisual> _enemies = new();
    private string _output = "";

    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            int outputIndex = Array.IndexOf(args, "--report-dir");
            if (outputIndex >= 0) _output = args[outputIndex + 1];
            bool capture = Array.IndexOf(args, "--capture") >= 0;
            if (capture && DisplayServer.GetName() == "headless") throw new Exception("Captura requer renderização.");
            var background = new Polygon2D { Polygon = new[] { Vector2.Zero, new Vector2(540, 0), new Vector2(540, 960), new Vector2(0, 960) }, Color = new Color(0.94f, 0.94f, 0.84f) };
            AddChild(background);
            LabelAt("Prévia dos recursos reais", new Vector2(270, 25), 22);
            LabelAt("Linha azul = apoio dos pés; mesma escala por folha", new Vector2(270, 58), 13);
            int count = 0;
            string[] lineages = { "bulbasaur", "charmander", "squirtle" };
            for (int row = 0; row < lineages.Length; row++)
            {
                var definition = GD.Load<PokemonDefinition>($"res://Resources/Pokemon/{lineages[row]}.tres");
                count += CheckFrames(definition.Frames, 384);
                for (int form = 0; form < 3; form++)
                {
                    var position = new Vector2(90 + form * 180, 155 + row * 150);
                    var visual = AddVisual(definition.Frames, definition.VisualScale, $"idle_{form}", position);
                    _pokemon.Add(visual);
                    LabelAt(definition.FormNames[form], position + new Vector2(0, 27), 16);
                }
            }
            string[] types = { "slime", "goblin", "golem", "ogre" };
            for (int i = 0; i < types.Length; i++)
            {
                var definition = GD.Load<EnemyDefinition>($"res://Resources/Enemies/{types[i]}.tres");
                count += CheckFrames(definition.Frames, 512);
                var position = new Vector2(70 + i * 132, 675);
                _enemies.Add(AddVisual(definition.Frames, definition.VisualScale, "move_front", position));
                LabelAt(definition.DisplayName, position + new Vector2(0, 28), 15);
            }
            LabelAt("Originais preservados; efeitos e variações pertencem às folhas", new Vector2(270, 800), 13);
            if (capture)
            {
                Directory.CreateDirectory(_output);
                await Save("art-idle");
                for (int i = 0; i < _pokemon.Count; i++) _pokemon[i].Attack($"attack_{i % 3}", false);
                foreach (var visual in _enemies) visual.Hit();
                await Frames(5);
                await Save("art-attack-hit");
                await Frames(5);
                await Save("art-attack-impact");
                foreach (var visual in _enemies) visual.Rest("move_right", false);
                await Frames(40);
                await Save("art-right");
                foreach (var visual in _enemies) visual.Rest("move_left", false);
                await Frames(20);
                await Save("art-left");
                foreach (var visual in _enemies) visual.Die();
                await Frames(28);
                await Save("art-death");
            }
            GD.Print($"ArtChecks: {count} frames validados; todos os recursos passaram.");
            TestExit.Quit(this);
        }
        catch (Exception error) { GD.PushError(error.ToString()); TestExit.Quit(this, 1); }
    }

    private static int CheckFrames(SpriteFrames frames, int canvas)
    {
        int count = 0;
        foreach (var name in frames.GetAnimationNames())
        {
            if (frames.GetFrameCount(name) == 0) throw new Exception("Animação vazia.");
            for (int i = 0; i < frames.GetFrameCount(name); i++)
            {
                var frame = frames.GetFrameTexture(name, i) as AtlasTexture ?? throw new Exception("Atlas ausente.");
                if (frame.GetSize() != new Vector2(canvas, canvas)) throw new Exception("Canvas muda entre frames.");
                var region = frame.Region;
                if (region.Position.X < 0 || region.Position.Y < 0 || region.End.X > frame.Atlas.GetWidth()
                    || region.End.Y > frame.Atlas.GetHeight() || !frame.FilterClip)
                    throw new Exception("Recorte fora da folha ou sem isolamento.");
                count++;
            }
        }
        return count;
    }

    private CharacterVisual AddVisual(SpriteFrames frames, float scale, string rest, Vector2 position)
    {
        var holder = new Node2D { Position = position };
        AddChild(holder);
        var baseline = new Line2D { Points = new[] { new Vector2(-50, 0), new Vector2(50, 0) }, Width = 1, DefaultColor = new Color(0.2f, 0.5f, 0.8f) };
        holder.AddChild(baseline);
        var visual = new CharacterVisual { TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        holder.AddChild(visual);
        visual.Configure(frames, scale, rest);
        return visual;
    }

    private void LabelAt(string text, Vector2 center, int size)
    {
        var label = new Label { Text = text, Position = center - new Vector2(260, 0), Size = new Vector2(520, 26), HorizontalAlignment = HorizontalAlignment.Center };
        // Name captions must stay within their columns, not a full-screen region.
        if (size >= 15 && size < 22) { label.Position = center - new Vector2(86, 0); label.Size = new Vector2(172, 28); }
        label.AddThemeColorOverride("font_color", new Color(0.1f, 0.22f, 0.18f));
        label.AddThemeFontSizeOverride("font_size", size);
        AddChild(label);
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Save(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png")) != Error.Ok)
            throw new Exception("Não foi possível salvar prévia.");
    }
}
