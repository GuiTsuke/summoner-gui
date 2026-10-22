using System;
using Godot;

public partial class CreatureSlot : Node2D
{
    public event Action<CreatureSlot>? Selected;
    public Creature? Occupant { get; private set; }
    public bool IsOccupied => GodotObject.IsInstanceValid(Occupant) && !Occupant!.IsQueuedForDeletion();
    private bool _selected;
    public override void _Ready() => GetNode<Button>("SelectButton").Pressed += () => Selected?.Invoke(this);
    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (IsOccupied) Occupant!.ShowRange = selected;
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (_selected)
            DrawArc(Vector2.Zero, 48, 0, Mathf.Tau, 64, new Color(1, 0.84f, 0.25f), 3, true);
    }
    public void PlaceCreature(PackedScene scene, PokemonDefinition definition)
    {
        Occupant = scene.Instantiate<Creature>();
        Occupant.Configure(definition);
        AddChild(Occupant);
        Occupant.ShowRange = _selected;
    }
    public static void MoveOrSwap(CreatureSlot source, CreatureSlot destination)
    {
        var first = source.Occupant!;
        var second = destination.IsOccupied ? destination.Occupant : null;
        source.Occupant = second;
        destination.Occupant = first;
        first.Reparent(destination, false);
        first.Position = Vector2.Zero;
        first.ResetTargetEffect();
        first.ShowRange = destination._selected;
        if (second != null)
        {
            second.Reparent(source, false);
            second.Position = Vector2.Zero;
            second.ResetTargetEffect();
            second.ShowRange = source._selected;
        }
    }
    public void Clear()
    {
        if (IsOccupied) { Occupant!.SetCombatEnabled(false); Occupant.QueueFree(); }
        Occupant = null;
        SetSelected(false);
    }
}
