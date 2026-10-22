using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ElementalChecks : Node
{
    private Main _main = null!;
    private GameSession _session = null!;
    private string _output = "";
    private bool _capture;

    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            int i = Array.IndexOf(args, "--report-dir");
            if (i >= 0) _output = args[i + 1];
            _capture = args.Contains("--capture");
            _main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>(); AddChild(_main);
            _session = _main.GetNode<GameSession>("GameSession"); _session.Director.SetProcess(false);
            await CheckAttacks();
            await CheckBurn();
            CheckEconomy();
            _session.Restart();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!GetTree().GetNodesInGroup(ElementalAttack.GroupName).Any(), "Reinício precisa limpar os ataques.");
            GD.Print("ElementalChecks: PASS - tres ataques, alvos, captura, queimadura, recompensa e economia.");
            TestExit.Quit(this);
        }
        catch (Exception e) { GD.PushError(e.ToString()); TestExit.Quit(this, 1); }
    }

    private Enemy Spawn(float progress = 100)
    {
        var enemy = GD.Load<PackedScene>("res://Scenes/Enemy.tscn").Instantiate<Enemy>();
        enemy.Configure(_session.Director.Waves[0].Composition[0], 20);
        _main.GetNode<Path2D>("EnemyPath").AddChild(enemy); enemy.SetProcess(false); enemy.Progress = progress;
        return enemy;
    }

    private Creature Make(string lineage, int slot)
    {
        _session.SelectSlot(_main.GetNode<CreatureSlot>($"Slots/Slot{slot}"));
        Require(_session.TrySummon(lineage), "Invocação da fixture falhou.");
        var unit = _session.SelectedSlot!.Occupant!; unit.SetProcess(false); return unit;
    }
    private ElementalAttack Launch(Creature unit)
    {
        var existing = unit.GetChildren().OfType<ElementalAttack>().ToArray();
        unit._Process(unit.AttackInterval + .01);
        var attack = unit.GetChildren().OfType<ElementalAttack>().Single(a => !existing.Contains(a));
        attack.SetProcess(false); return attack;
    }
    private async Task CheckAttacks()
    {
        _session.StartingCoins = 5000; _session.Restart();
        foreach (string lineage in new[] { "bulbasaur", "charmander", "squirtle" })
        {
            _session.Restart();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var unit = Make(lineage, 5);
            // Private attack definition permits deterministic burn tests without changing resources.
            var definition = (PokemonDefinition)unit.Definition.Duplicate();
            definition.Attack = (ElementalAttackDefinition)definition.Attack.Duplicate();
            definition.Attack.BurnBaseChance = 0; definition.Attack.BurnChancePerLevel = 0;
            unit.Configure(definition);
            var enemy = Spawn(738);
            float hp = enemy.CurrentHealth;
            var attack = Launch(unit);
            Require(enemy.CurrentHealth == hp, "Lançamento não pode aplicar dano.");
            attack._Process(.08);
            Require(enemy.CurrentHealth == hp, "Ataque não pode atingir antes do contato.");
            await Capture("element-" + lineage + "-advancing");
            enemy.Progress += 12; // A moving target remains valid; homing/extension follows its new position.
            attack._Process(.5);
            Require(attack.IsResolved && Mathf.IsEqualApprox(enemy.CurrentHealth, hp - unit.Damage), "Contato precisa aplicar um único dano real.");
            attack._Process(.1); attack._Process(.1);
            Require(Mathf.IsEqualApprox(enemy.CurrentHealth, hp - unit.Damage) && !enemy.IsBurning, "Frames extras não devem causar dano ou queimadura com chance zero.");
            var cancelled = Launch(unit); enemy.TakeDamage(float.MaxValue); cancelled._Process(.5);
            Require(cancelled.IsResolved && cancelled.IsQueuedForDeletion(), "Alvo morto deve cancelar o ataque sem impacto.");
            var removed = Spawn(); unit.GlobalPosition = removed.GlobalPosition + new Vector2(-70, 0);
            var detached = Launch(unit); removed.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            detached._Process(.5); Require(detached.IsResolved, "Referência removida deve ser descartada.");
            var moving = Spawn(); unit.GlobalPosition = moving.GlobalPosition + new Vector2(-70, 0);
            var snapshot = Launch(unit); float captured = snapshot.CapturedDamage, before = moving.CurrentHealth;
            for (int upgrade = unit.Level; upgrade < 4; upgrade++) unit.Upgrade();
            unit.Evolve();
            snapshot._Process(.5);
            Require(Mathf.IsEqualApprox(moving.CurrentHealth, before - captured) && unit.Damage > captured,
                "Melhoria/evolução durante voo não pode alterar o dano capturado.");
            moving.Cancel();
        }
        _session.Restart(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var vine = Make("bulbasaur", 2); var escaping = Spawn(); vine.GlobalPosition = escaping.GlobalPosition;
        var whip = Launch(vine); escaping.GlobalPosition += new Vector2(vine.Range + 1, 0);
        float initial = escaping.CurrentHealth; whip._Process(.5);
        Require(escaping.CurrentHealth == initial && whip.IsResolved, "Cipós devem validar alcance no contato.");
        escaping.Cancel();
        _session.Restart(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CheckBurn()
    {
        var fire = Make("charmander", 5);
        var definition = (PokemonDefinition)fire.Definition.Duplicate();
        definition.Attack = (ElementalAttackDefinition)definition.Attack.Duplicate();
        definition.Attack.BurnBaseChance = 1; definition.Attack.BurnChanceCap = 1; fire.Configure(definition);
        var enemy = Spawn(); fire.GlobalPosition = enemy.GlobalPosition + new Vector2(-60, 0);
        Launch(fire)._Process(.5);
        Require(enemy.IsBurning, "Chance 100% precisa aplicar queimadura.");
        float hp = enemy.CurrentHealth;
        enemy.AdvanceBurn(3);
        Require(!enemy.IsBurning && Mathf.IsEqualApprox(enemy.CurrentHealth, hp - 3), "Queimadura inicial deve dar três ticks de 1 e expirar.");
        enemy.ApplyBurn(4, 3, 1); enemy.AdvanceBurn(.7); enemy.ApplyBurn(2, 3, 1); enemy.AdvanceBurn(.31);
        Require(Mathf.IsEqualApprox(enemy.CurrentHealth, hp - 7), "Reaplicação deve preservar dano maior e cadência.");
        enemy.ApplyBurn(6, 3, 1); hp = enemy.CurrentHealth; enemy.AdvanceBurn(3);
        Require(!enemy.IsBurning && Mathf.IsEqualApprox(enemy.CurrentHealth, hp - 18), "Estado único deve renovar e usar o maior dano, sem empilhar.");
        enemy.ApplyBurn(2, 3, 1); enemy.AdvanceBurn(1); await Capture("element-burn");
        enemy.Cancel();
        _session.Restart(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _session.TryStartWave(); _session.Director._Process(0);
        var rewarded = _main.GetNode<Path2D>("EnemyPath").GetChildren().OfType<Enemy>().Single(e => e.IsTargetable);
        rewarded.SetProcess(false); int coins = _session.Coins, deaths = 0;
        rewarded.Defeated += () => deaths++;
        rewarded.ApplyBurn(rewarded.MaxHealth, 3, 1); rewarded.AdvanceBurn(1); rewarded.AdvanceBurn(5); rewarded.TakeDamage(999);
        rewarded.ApplyBurn(999, 3, 1);
        Require(deaths == 1 && _session.Coins == coins + rewarded.Reward && !rewarded.IsBurning,
            "Morte por queimadura precisa resolver recompensa única e limpar estado.");
        _session.Restart(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(!GodotObject.IsInstanceValid(rewarded), "Reinício deve remover estado/visual da queimadura.");
    }

    private void CheckEconomy()
    {
        int totalRewards = _session.Director.Waves.Sum(w => w.Composition.Sum(e => e.Reward));
        int budget = 150 + totalRewards;
        // A real legal level/evolution purchase sequence measured by the session's balance.
        _session.StartingCoins = budget; _session.Restart(); var unit = Make("bulbasaur", 2);
        Make("charmander", 5); Make("squirtle", 8);
        _session.SelectSlot(_main.GetNode<CreatureSlot>("Slots/Slot2"));
        while (unit.Level < 7) Require(_session.TryUpgrade(), "Orçamento total precisa permitir níveis da unidade prioritária.");
        Require(unit.FormIndex == 0 && _session.TryEvolve() && _session.TryEvolve(), "Nível sete deve permitir duas compras separadas.");
        Require(budget - _session.Coins == 1225 && unit.FormIndex == 2 && unit.Level == 7,
            "Orçamento de três invocações + nível sete + duas evoluções deve custar 1225.");
        GD.Print($"ElementalChecks: orçamento máximo {budget}, custo prioritário 1225, sobra {_session.Coins}; não mede viabilidade de uma partida.");
    }

    private async Task Capture(string name)
    {
        if (!_capture) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Directory.CreateDirectory(_output);
        Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png")) == Error.Ok, "Falha na captura.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
