using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameSession : Node
{
    public enum MatchState { Preparing, Fighting, Victory, Defeat }
    [Export] public WaveDirector Director { get; set; } = null!;
    [Export] public Node2D SlotsRoot { get; set; } = null!;
    [Export] public PackedScene CreatureScene { get; set; } = null!;
    [Export] public Godot.Collections.Array<PokemonDefinition> Pokemon { get; set; } = new();
    [Export] public int StartingCoins { get; set; } = 150;
    [Export] public int StartingLives { get; set; } = 5;
    [Export] public int SummonCost { get; set; } = 50;
    [Export] public int UpgradeBaseCost { get; set; } = 25;
    [Export] public int MaxLevel { get; set; } = 10;
    public event Action? Changed;
    public event Action<string>? SoundRequested;
    public event Action<bool>? MatchFinished;
    public event Action? Restarted;
    public int Coins { get; private set; }
    public int Lives { get; private set; }
    public MatchState State { get; private set; }
    public CreatureSlot? SelectedSlot { get; private set; }
    public string Message { get; private set; } = "";
    public bool CanAct => State == MatchState.Preparing || State == MatchState.Fighting;
    public bool CanStartWave => CanAct && !Director.IsRunning && Director.CurrentWave < Director.WaveCount;
    public bool CanReposition => State == MatchState.Preparing && !Director.IsRunning;
    public bool IsRepositioning { get; private set; }
    public CreatureSlot? MoveSource { get; private set; }
    public int NextUpgradeCost => SelectedSlot?.IsOccupied == true
        ? Mathf.Max(0, UpgradeBaseCost) * SelectedSlot.Occupant!.Level : 0;
    public int NextEvolutionCost => SelectedSlot?.IsOccupied == true
        && SelectedSlot.Occupant!.FormIndex + 1 < SelectedSlot.Occupant.Definition.FormNames.Length
        ? Mathf.Max(0, SelectedSlot.Occupant.Definition.EvolutionCosts[SelectedSlot.Occupant.FormIndex + 1]) : 0;
    private readonly List<CreatureSlot> _slots = new();

    public override void _Ready()
    {
        foreach (var slot in SlotsRoot.GetChildren().OfType<CreatureSlot>())
        {
            _slots.Add(slot);
            slot.Selected += SelectSlot;
        }
        Director.EnemyResolved += OnEnemyResolved;
        Director.WaveCompleted += OnWaveCompleted;
        Director.Changed += () => Changed?.Invoke();
        Restart();
    }

    public bool IsLineageOwned(string lineage) =>
        _slots.Any(slot => slot.IsOccupied && slot.Occupant!.Lineage == lineage);

    public void SelectSlot(CreatureSlot slot)
    {
        if (!CanAct || !_slots.Contains(slot)) return;
        SoundRequested?.Invoke("ui_select");
        if (IsRepositioning)
        {
            if (!CanReposition) { CancelReposition(); return; }
            if (MoveSource == null)
            {
                if (!slot.IsOccupied) { Reject("Selecione um Pokémon para mover."); return; }
                MoveSource = slot;
                SelectOnly(slot);
                Message = "Escolha um destino vazio ou outro Pokémon para trocar.";
            }
            else
            {
                if (MoveSource == slot) { CancelReposition(); return; }
                CreatureSlot.MoveOrSwap(MoveSource, slot);
                IsRepositioning = false;
                MoveSource = null;
                SelectOnly(slot);
                Message = "Posição atualizada.";
            }
        }
        else
        {
            SelectOnly(slot);
            Message = Coins < (slot.IsOccupied ? NextUpgradeCost : SummonCost) ? "Moedas insuficientes." : "";
        }
        Changed?.Invoke();
    }

    private void SelectOnly(CreatureSlot slot)
    {
        SelectedSlot?.SetSelected(false);
        SelectedSlot = slot;
        slot.SetSelected(true);
    }

    public bool BeginReposition()
    {
        if (!CanReposition) return Reject("Reposicionamento disponível apenas entre ondas.");
        IsRepositioning = true;
        MoveSource = null;
        Message = "Selecione o Pokémon que deseja reposicionar.";
        Changed?.Invoke();
        return true;
    }

    public void CancelReposition()
    {
        IsRepositioning = false;
        MoveSource = null;
        Message = "Reposicionamento cancelado.";
        Changed?.Invoke();
    }

    public bool TrySummon() => TrySummon(Pokemon.FirstOrDefault(type => !IsLineageOwned(type.Lineage))?.Lineage ?? "");
    public bool TrySummon(string lineage)
    {
        if (!CanAct || IsRepositioning || SelectedSlot == null) return false;
        if (SelectedSlot.IsOccupied) return Reject("Este espaço já está ocupado.");
        var definition = Pokemon.FirstOrDefault(type => type.Lineage == lineage);
        if (definition == null) return Reject("Escolha um Pokémon disponível.");
        if (IsLineageOwned(lineage)) return Reject("Esta linhagem já foi invocada.");
        int cost = Mathf.Max(0, SummonCost);
        if (Coins < cost) return Reject("Moedas insuficientes.");
        SelectedSlot.PlaceCreature(CreatureScene, definition);
        Coins -= cost;
        Message = $"{definition.FormNames[0]} invocado!";
        SoundRequested?.Invoke("summon");
        Changed?.Invoke();
        return true;
    }

    public bool TryUpgrade()
    {
        if (!CanAct || IsRepositioning || SelectedSlot?.IsOccupied != true) return false;
        var creature = SelectedSlot.Occupant!;
        if (creature.Level >= Mathf.Max(1, MaxLevel)) return Reject("Nível máximo.");
        int cost = NextUpgradeCost;
        if (Coins < cost) return Reject("Moedas insuficientes.");
        Coins -= cost;
        creature.Upgrade();
        SoundRequested?.Invoke("upgrade");
        Message = $"{creature.DisplayName} chegou ao nível {creature.Level}!";
        Changed?.Invoke();
        return true;
    }

    public bool TryEvolve()
    {
        if (!CanAct || IsRepositioning || SelectedSlot?.IsOccupied != true) return false;
        var unit = SelectedSlot.Occupant!;
        int next = unit.FormIndex + 1;
        if (next >= unit.Definition.FormNames.Length) return Reject("Forma final alcançada.");
        if (unit.Level < unit.Definition.EvolutionLevels[next])
            return Reject($"Evolução exige nível {unit.Definition.EvolutionLevels[next]}.");
        int cost = NextEvolutionCost;
        if (Coins < cost) return Reject("Moedas insuficientes para evoluir.");
        Coins -= cost;
        unit.Evolve();
        Message = $"Evoluiu para {unit.DisplayName}!";
        SoundRequested?.Invoke("evolve");
        Changed?.Invoke();
        return true;
    }

    public bool TryStartWave()
    {
        if (!CanStartWave) return false;
        string error = Director.GetConfigurationError();
        if (error != "") return Reject(error);
        IsRepositioning = false;
        MoveSource = null;
        if (!Director.TryStartNextWave()) return false;
        State = MatchState.Fighting;
        SoundRequested?.Invoke("wave_start");
        Message = "Defenda o baú!";
        Changed?.Invoke();
        return true;
    }

    private void OnEnemyResolved(Enemy enemy, bool arrived)
    {
        if (!CanAct) return;
        if (arrived)
        {
            Lives = enemy.IsBoss ? 0 : Mathf.Max(0, Lives - 1);
            Message = enemy.IsBoss ? "O Rei Ogro alcançou o baú!" : "Um inimigo chegou ao baú!";
            if (Lives == 0) Finish(false);
        }
        else
        {
            int reward = Mathf.Max(0, enemy.Reward);
            Coins += reward;
            if (reward > 0) SoundRequested?.Invoke("coin");
            Message = $"{enemy.Definition.DisplayName} derrotado: +{reward} moedas.";
        }
        Changed?.Invoke();
    }

    private void OnWaveCompleted()
    {
        if (!CanAct) return;
        if (Director.CurrentWave == Director.WaveCount)
        {
            if (Director.BossDefeated && Lives > 0) Finish(true);
            else Finish(false);
        }
        else { State = MatchState.Preparing; Message = "Onda concluída. Melhore ou reposicione."; }
        Changed?.Invoke();
    }

    private void Finish(bool victory)
    {
        State = victory ? MatchState.Victory : MatchState.Defeat;
        IsRepositioning = false;
        MoveSource = null;
        Director.Stop();
        foreach (var slot in _slots)
            if (slot.IsOccupied) slot.Occupant!.SetCombatEnabled(false);
        Message = victory ? "Vitória! O Rei Ogro foi derrotado." : "Derrota! O baú ficou sem vidas.";
        MatchFinished?.Invoke(victory);
    }

    public void Restart()
    {
        Director.Reset();
        foreach (var slot in _slots) slot.Clear();
        SelectedSlot = null;
        IsRepositioning = false;
        MoveSource = null;
        Coins = Mathf.Max(0, StartingCoins);
        Lives = Mathf.Max(1, StartingLives);
        State = MatchState.Preparing;
        Message = "Selecione uma base e invoque seu Pokémon.";
        Restarted?.Invoke();
        Changed?.Invoke();
    }

    private bool Reject(string message)
    {
        Message = message;
        SoundRequested?.Invoke("denied");
        Changed?.Invoke();
        return false;
    }
}
