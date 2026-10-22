using Godot;

[Tool, GlobalClass]
public partial class PokemonDefinition : Resource
{
    [Export] public string Lineage { get; set; } = "bulbasaur";
    [Export] public string[] FormNames { get; set; } = { "Bulbasaur", "Ivysaur", "Venusaur" };
    [Export] public int[] EvolutionLevels { get; set; } = { 1, 4, 7 };
    [Export] public int[] EvolutionCosts { get; set; } = { 0, 150, 400 };
    [Export] public float[] FormMultipliers { get; set; } = { 1, 2, 3.5f };
    [Export] public string[] FormAbilityNotes { get; set; } = { "", "", "" };
    [Export] public ElementalAttackDefinition Attack { get; set; } = null!;
    [Export] public float BaseDamage { get; set; } = 20;
    [Export] public float LevelDamageGrowth { get; set; } = 0.20f;
    [Export] public float Range { get; set; } = 100;
    [Export] public float AttackInterval { get; set; } = 1.3f;
    [Export] public SpriteFrames Frames { get; set; } = null!;
    [Export] public float VisualScale { get; set; } = 0.24f;

    public float CalculateDamage(int level, int form = 0) => BaseDamage
        * (1 + LevelDamageGrowth * (Mathf.Max(1, level) - 1)) * FormMultipliers[form];

    private Texture2D? _portrait;
    public Texture2D Portrait
    {
        get
        {
            if (_portrait != null) return _portrait;
            var frame = (AtlasTexture)Frames.GetFrameTexture("idle_0", 0);
            return _portrait = new AtlasTexture { Atlas = frame.Atlas, Region = frame.Region, FilterClip = true };
        }
    }
}
