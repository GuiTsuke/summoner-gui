using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

// An action driver, not a second game implementation. Rules remain in Main's nodes.
public partial class SimulationChecks : Node
{
    private double _seconds;
    private string _output = "";
    private bool _capture;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public override void _Process(double delta) => _seconds += delta;

    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            string Argument(string name, string fallback)
            {
                int index = Array.IndexOf(args, name);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
            }
            _output = ProjectSettings.GlobalizePath(Argument("--report-dir", "user://validation"));
            _capture = args.Contains("--capture");
            if (_capture && DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Capturas exigem execução com renderização real.");
            Directory.CreateDirectory(_output);
            string scenarioFile = ProjectSettings.GlobalizePath(Argument(
                "--scenarios", "res://Tests/Scenarios/simulation.json"));
            var scenarios = JsonSerializer.Deserialize<List<Scenario>>(
                File.ReadAllText(scenarioFile), JsonOptions) ?? throw new InvalidOperationException("Cenários ausentes.");
            if (scenarios.Count == 0)
                throw new InvalidOperationException("A lista de cenários está vazia.");

            var results = new List<Result>();
            foreach (var scenario in scenarios)
            {
                var result = await Play(scenario);
                results.Add(result);
                File.WriteAllText(Path.Combine(_output, "simulation.json"),
                    JsonSerializer.Serialize(results, JsonOptions));
                GD.Print($"Simulação {result.Name}: {result.Outcome}, onda {result.WaveReached}/{result.ConfiguredWaves}, "
                    + $"moedas {result.Coins}, vidas {result.Lives}, mortes {result.Kills}, "
                    + $"chegadas {result.Arrivals}, melhorias {result.Upgrades}, tempo {result.Seconds:0.00} s.");
            }
            Require(results.All(result => result.BaselineMatches),
                "Regressão: resultado diferente do esperado ou ações programadas não alcançadas. Consulte simulation.json.");
            GD.Print("SimulationChecks: todos os cenários executados e conciliados.");
            TestExit.Quit(this);
        }
        catch (Exception error)
        {
            if (!string.IsNullOrEmpty(_output))
                File.WriteAllText(Path.Combine(_output, "simulation-error.txt"), error.ToString());
            GD.PushError(error.ToString());
            TestExit.Quit(this, 1);
        }
    }

    private async Task<Result> Play(Scenario scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.Name)
            || scenario.Name.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
            throw new InvalidOperationException("Nome de cenário inválido.");

        var main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
        AddChild(main);
        var session = main.GetNode<GameSession>("GameSession");
        var director = session.Director;
        var result = new Result
        {
            Name = scenario.Name, InitialCoins = session.Coins, InitialLives = session.Lives,
            ConfiguredWaves = director.WaveCount
        };
        int previousLives = session.Lives;
        director.EnemySpawned += enemy =>
        {
            result.Spawned++;
            if (enemy.IsBoss) result.BossesSpawned++;
            result.Spawns.Add(new { wave = director.CurrentWave, type = enemy.Definition.Id,
                enemy.IsBoss, enemy.MaxHealth, enemy.Speed, enemy.Reward, pending = director.PendingSpawns });
        };
        director.EnemyResolved += (enemy, arrived) =>
        {
            if (arrived)
            {
                result.Arrivals++;
                if (enemy.IsBoss) result.BossArrivals++;
                result.LivesLost += previousLives - session.Lives;
            }
            else
            {
                result.Kills++;
                if (enemy.IsBoss) result.BossesDefeated++;
                result.Earned += enemy.Reward;
            }
            previousLives = session.Lives;
        };

        await Capture(main, scenario.Name + "-initial");
        double started = _seconds;
        var executed = new HashSet<PlannedAction>();
        while (session.CanAct)
        {
            int nextWave = director.CurrentWave + 1;
            foreach (var action in scenario.Actions.Where(item => item.Wave == nextWave && item.Seconds < 0))
            {
                Execute(main, session, action, result, 0);
                executed.Add(action);
            }
            if (!session.TryStartWave())
                throw new InvalidOperationException($"{scenario.Name}: não foi possível iniciar onda {nextWave}.");
            double waveStarted = _seconds;
            result.Waves.Add(Snapshot(session, "start", 0));
            bool capturedCombat = false;
        bool capturedEvolution = false;
        bool capturedBoss = false;
            while (director.IsRunning && session.CanAct)
            {
                double elapsed = _seconds - waveStarted;
                if (elapsed > 180)
                    throw new TimeoutException($"{scenario.Name}: onda {director.CurrentWave} excedeu 180 s simulados.");
                foreach (var action in scenario.Actions.Where(item => item.Wave == director.CurrentWave
                    && item.Seconds >= 0 && item.Seconds <= elapsed && !executed.Contains(item)))
                {
                    Execute(main, session, action, result, elapsed);
                    executed.Add(action);
                }
                if (_capture && !capturedCombat && elapsed >= 3 && director.CurrentWave == 1)
                {
                    await Capture(main, scenario.Name + "-combat");
                    capturedCombat = true;
                }
                if (_capture && !capturedEvolution && director.CurrentWave == 4 && elapsed >= 1)
                {
                    await Capture(main, scenario.Name + "-evolution");
                    capturedEvolution = true;
                }
                if (_capture && !capturedBoss && director.ActiveBoss != null)
                {
                    await Capture(main, scenario.Name + "-boss");
                    capturedBoss = true;
                }
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            result.Waves.Add(Snapshot(session, "end", _seconds - waveStarted));
        }
        result.Seconds = Math.Round(_seconds - started, 3);
        result.Outcome = session.State.ToString();
        result.WaveReached = director.CurrentWave;
        result.Coins = session.Coins;
        result.Lives = session.Lives;
        result.Cancelled = result.Spawned - result.Kills - result.Arrivals;
        result.UnreachedActions = scenario.Actions.Count - executed.Count;
        result.ExpectedOutcome = scenario.ExpectedOutcome;
        result.BaselineMatches = (string.IsNullOrEmpty(scenario.ExpectedOutcome)
            || result.Outcome == scenario.ExpectedOutcome)
            && (!scenario.RequireAllActionsReached || result.UnreachedActions == 0);
        foreach (var slot in session.SlotsRoot.GetChildren().OfType<CreatureSlot>().Where(slot => slot.IsOccupied))
            result.Units.Add(new { slot = slot.Name.ToString(), slot.Occupant!.Lineage,
                slot.Occupant.DisplayName, slot.Occupant.Level, slot.Occupant.FormIndex, slot.Occupant.Damage });

        // Accounting assertions observe costs/rewards; they do not simulate any game rule.
        Require(result.Coins == result.InitialCoins - result.Spent + result.Earned,
            "Saldo final não confere com os débitos e recompensas observados.");
        Require(result.Lives == result.InitialLives - result.LivesLost,
            "Vidas finais não conferem com as chegadas observadas.");
        Require(result.Outcome == "Victory" || result.Outcome == "Defeat", "A simulação não terminou.");
        Require(result.Cancelled >= 0, "Mais resultados de inimigos do que surgimentos observados.");
        Require(result.ConfiguredWaves == 10 && result.BossesSpawned <= 1,
            "Configuração precisa ter dez ondas e no máximo um chefe surgido.");
        if (result.Outcome == "Victory")
            Require(result.WaveReached == 10 && result.BossesDefeated == 1,
                "Vitória precisa resolver a onda 10 e derrotar o chefe.");
        Require(!scenario.RequireUpgrade || result.Upgrades > 0, "Cenário não exercitou melhorias com sucesso.");
        await Capture(main, scenario.Name + "-result");
        main.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return result;
    }

    private static void Execute(Main main, GameSession session, PlannedAction action, Result result, double time)
    {
        if (action.Slot < 1 || action.Slot > 9 || (action.Kind != "summon" && action.Kind != "upgrade" && action.Kind != "move"))
            throw new InvalidOperationException("Ação programada inválida.");
        var slot = main.GetNode<CreatureSlot>($"Slots/Slot{action.Slot}");
        int coinsBefore = session.Coins;
        var previous = slot.Occupant;
        int previousLevel = previous?.Level ?? 0;
        bool succeeded;
        if (action.Kind == "move")
        {
            if (action.TargetSlot < 1 || action.TargetSlot > 9) throw new InvalidOperationException("Destino inválido.");
            succeeded = session.BeginReposition();
            if (succeeded)
            {
                session.SelectSlot(slot);
                session.SelectSlot(main.GetNode<CreatureSlot>($"Slots/Slot{action.TargetSlot}"));
                succeeded = !session.IsRepositioning;
            }
        }
        else
        {
            session.SelectSlot(slot);
            succeeded = action.Kind == "summon" ? session.TrySummon(action.Lineage) : session.TryUpgrade();
        }
        int spent = coinsBefore - session.Coins;
        result.Spent += spent;
        if (succeeded)
        {
            if (action.Kind == "summon") result.Summons++;
            else if (action.Kind == "upgrade")
            {
                result.Upgrades++;
                Require(slot.Occupant == previous && previous!.Level == previousLevel + 1,
                    "A melhoria deve preservar a unidade e avançar o nível.");
            }
        }
        else Require(spent == 0, "Ação recusada não pode debitar moedas.");
        result.Actions.Add(new
        {
            action.Wave, seconds = Math.Round(time, 3), action.Kind, action.Slot, succeeded,
            coinsBefore, coinsAfter = session.Coins, spent,
            level = slot.Occupant?.Level, damage = slot.Occupant?.Damage, message = session.Message
        });
    }

    private static object Snapshot(GameSession session, string phase, double elapsed) => new
    {
        wave = session.Director.CurrentWave, phase, seconds = Math.Round(elapsed, 3),
        session.Coins, session.Lives, remaining = session.Director.Remaining, state = session.State.ToString()
    };

    private async Task Capture(Main main, string name)
    {
        if (!_capture) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png"));
        Require(error == Error.Ok, "Não foi possível salvar captura.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public sealed class Scenario
    {
        public string Name { get; set; } = "";
        public bool RequireUpgrade { get; set; }
        public string ExpectedOutcome { get; set; } = "";
        public bool RequireAllActionsReached { get; set; } = true;
        public List<PlannedAction> Actions { get; set; } = new();
    }

    public sealed class PlannedAction
    {
        public int Wave { get; set; }
        public double Seconds { get; set; } = -1;
        public string Kind { get; set; } = "";
        public string Lineage { get; set; } = "bulbasaur";
        public int Slot { get; set; }
        public int TargetSlot { get; set; }
    }

    public sealed class Result
    {
        public string Name { get; set; } = "";
        public string Outcome { get; set; } = "";
        public string ExpectedOutcome { get; set; } = "";
        public bool BaselineMatches { get; set; }
        public int ConfiguredWaves { get; set; }
        public int WaveReached { get; set; }
        public int InitialCoins { get; set; }
        public int InitialLives { get; set; }
        public int Coins { get; set; }
        public int Lives { get; set; }
        public int Summons { get; set; }
        public int Upgrades { get; set; }
        public int Kills { get; set; }
        public int Arrivals { get; set; }
        public int LivesLost { get; set; }
        public int BossesSpawned { get; set; }
        public int BossesDefeated { get; set; }
        public int BossArrivals { get; set; }
        public int Spawned { get; set; }
        public int Cancelled { get; set; }
        public int Spent { get; set; }
        public int Earned { get; set; }
        public double Seconds { get; set; }
        public int UnreachedActions { get; set; }
        public List<object> Actions { get; set; } = new();
        public List<object> Waves { get; set; } = new();
        public List<object> Units { get; set; } = new();
        public List<object> Spawns { get; set; } = new();
    }
}
