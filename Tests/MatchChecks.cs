using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MatchChecks : Node
{
    public override async void _Ready()
    {
        try
        {
            await CheckConfigurationAndMouse();
            await CheckPurchasesAndEvolution();
            await CheckMovement();
            await CheckWaves();
            await CheckFinalDeathAnimationAndReset();
            await CheckDefeatAndRestart();
            await CheckCancellationWithPendingSpawns();
            GD.Print("MatchChecks: todos os testes passaram."); TestExit.Quit(this);
        }
        catch (Exception error) { GD.PushError(error.ToString()); TestExit.Quit(this, 1); }
    }
    private Main Create()
    {
        var main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>(); AddChild(main);
        Session(main).Director.SetProcess(false); return main;
    }
    private static GameSession Session(Main main) => main.GetNode<GameSession>("GameSession");
    private static CreatureSlot Slot(Main main, int index) => main.GetNode<CreatureSlot>($"Slots/Slot{index}");
    private static Enemy[] Enemies(Main main) => main.GetNode<Path2D>("EnemyPath").GetChildren().OfType<Enemy>().Where(e => e.IsTargetable).ToArray();
    private async Task Dispose(Main main) { Session(main).Restart(); main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void SmallWaves(Main main, bool companion = false)
    {
        var director = Session(main).Director;
        var definitions = director.Waves.SelectMany(w => w.Composition).ToArray();
        var ordinary = definitions.First(e => !e.IsBoss); var boss = definitions.First(e => e.IsBoss);
        var waves = new Godot.Collections.Array<WaveDefinition>();
        for (int i = 0; i < 10; i++)
        {
            var wave = new WaveDefinition();
            if (i == 9 && companion) wave.Composition.Add(ordinary);
            wave.Composition.Add(i == 9 ? boss : ordinary); waves.Add(wave);
        }
        director.Waves = waves; Session(main).Restart();
        Require(string.IsNullOrEmpty(director.GetConfigurationError()), "Cenário privado deve ser válido.");
    }
    private async Task CheckConfigurationAndMouse()
    {
        var main = Create(); var s = Session(main); var d = s.Director;
        Require(s.Coins == 150 && s.Lives == 5 && d.WaveCount == 10, "Estado inicial incorreto.");
        Require(s.Pokemon.Count == 3 && s.Pokemon.Select(p => p.Lineage).Distinct().Count() == 3, "Catálogo deve ter três linhagens distintas.");
        Require(d.Waves.SelectMany(w => w.Composition).Count(e => e.IsBoss) == 1
            && d.Waves.Last().Composition.Last().IsBoss && string.IsNullOrEmpty(d.GetConfigurationError()), "Chefe deve ser único e último da décima onda.");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().NotifyMouseEntered();
        for (int i = 1; i <= 9; i++) { Click(Slot(main, i).GlobalPosition); Require(s.SelectedSlot == Slot(main, i), $"Clique no espaço {i} falhou."); }
        Click(Slot(main, 2).GlobalPosition);
        Click(main.GetNode<Button>("HUD/Interface/Bottom/Rows/Portraits/Summon0").GetGlobalRect().GetCenter());
        Require(Slot(main, 2).IsOccupied && s.Coins == 100, "Retrato deve invocar por 50 moedas.");
        Click(main.GetNode<Button>("HUD/Interface/Bottom/Rows/Actions/SelectionAction").GetGlobalRect().GetCenter());
        Require(Slot(main, 2).Occupant!.Level == 2 && s.Coins == 75, "Botão deve melhorar seleção.");
        Click(main.GetNode<Button>("HUD/Interface/Bottom/Rows/Actions/StartWave").GetGlobalRect().GetCenter());
        Require(d.IsRunning && d.CurrentWave == 1, "Botão deve iniciar onda."); await Dispose(main);
    }
    private async Task CheckPurchasesAndEvolution()
    {
        var main = Create(); var s = Session(main);
        Require(!s.TrySummon("bulbasaur") && !s.TryUpgrade() && !s.TryEvolve(), "Ação sem seleção deve falhar.");
        s.StartingCoins = 10000; s.Restart(); s.SelectSlot(Slot(main, 2));
        Require(s.TrySummon("bulbasaur"), "Primeira linhagem deve poder ser invocada.");
        var creature = Slot(main, 2).Occupant!; var position = creature.GlobalPosition;
        float range = creature.Range, interval = creature.AttackInterval;
        Require(range == 100 && Mathf.IsEqualApprox(interval, 1.3f), "Atributos iniciais de Bulbasaur incorretos.");
        for (int level = 1; level <= 10; level++)
        {
            int previousForm = level > 7 ? 2 : level > 4 ? 1 : 0;
            Require(creature.FormIndex == previousForm, "Melhorar nível não pode evoluir automaticamente.");
            int beforeEvolution = s.Coins;
            if (level == 4 || level == 7)
            {
                int cost = level == 4 ? 150 : 400;
                Require(s.NextEvolutionCost == cost && s.TryEvolve() && s.Coins == beforeEvolution - cost
                    && Slot(main, 2).Occupant == creature && creature.Level == level,
                    "Evolução deve ser compra separada e preservar unidade/nível.");
                int paidBalance = s.Coins;
                Require(!s.TryEvolve() && s.Coins == paidBalance,
                    "Evolução duplicada ou sem requisito não pode cobrar novamente.");
            }
            else Require(!s.TryEvolve() && s.Coins == beforeEvolution,
                "Evolução exige nível mínimo e forma disponível.");
            int form = level >= 7 ? 2 : level >= 4 ? 1 : 0;
            float damage = 20f * (1 + .2f * (level - 1)) * (form == 2 ? 3.5f : form == 1 ? 2 : 1);
            Require(creature.Level == level && creature.FormIndex == form && Mathf.IsEqualApprox(creature.Damage, damage)
                && creature.Lineage == "bulbasaur" && creature.GlobalPosition == position && creature.Range == range && creature.AttackInterval == interval,
                $"Evolução/atributos incorretos no nível {level}.");
            s.SelectSlot(Slot(main, 3)); int balance = s.Coins;
            Require(!s.TrySummon("bulbasaur") && s.Coins == balance && !Slot(main, 3).IsOccupied, "Duplicação de linhagem deve ser bloqueada inclusive após evolução.");
            s.SelectSlot(Slot(main, 2));
            if (level < 10) Require(s.NextUpgradeCost == 25 * level && s.TryUpgrade() && s.Coins == balance - 25 * level
                && Slot(main, 2).Occupant == creature, "Melhoria deve preservar unidade e cobrar custo correto.");
        }
        int finalBalance = s.Coins;
        Require(!s.TryUpgrade() && creature.Level == 10 && s.Coins == finalBalance, "Nível máximo deve ser dez.");
        s.SelectSlot(Slot(main, 3)); Require(s.TrySummon("charmander"), "Outra linhagem deve ser permitida.");
        var fire = Slot(main, 3).Occupant!;
        Require(fire.Damage == 10 && fire.Range == 120 && Mathf.IsEqualApprox(fire.AttackInterval, .65f), "Atributos de Charmander incorretos.");
        s.SelectSlot(Slot(main, 4)); Require(s.TrySummon("squirtle"), "Terceira linhagem deve ser permitida.");
        var water = Slot(main, 4).Occupant!;
        Require(water.Damage == 15 && water.Range == 155 && water.AttackInterval == 1, "Atributos de Squirtle incorretos.");
        s.SelectSlot(Slot(main, 5)); Require(!s.TrySummon(), "Catálogo esgotado não pode criar quarta unidade.");
        s.StartingCoins = 50; s.Restart(); s.SelectSlot(Slot(main, 2));
        Require(s.TrySummon("bulbasaur") && s.Coins == 0, "Invocação deve consumir saldo exato.");
        creature = Slot(main, 2).Occupant!;
        Require(!s.TryUpgrade() && creature.Level == 1 && creature.Damage == 20 && s.Coins == 0, "Melhoria sem saldo não pode alterar unidade.");
        s.SelectSlot(Slot(main, 3)); Require(!s.TrySummon("charmander") && !Slot(main, 3).IsOccupied && s.Coins == 0, "Invocação sem saldo não pode alterar tabuleiro.");
        // 50 summon + 25 + 50 + 75 upgrades leaves 149: level met, evolution unaffordable.
        s.StartingCoins = 349; s.Restart(); s.SelectSlot(Slot(main, 2)); s.TrySummon("bulbasaur");
        for (int i = 0; i < 3; i++) Require(s.TryUpgrade(), "Fixture precisa chegar ao nível quatro.");
        creature = Slot(main, 2).Occupant!;
        Require(s.Coins == 149 && !s.TryEvolve() && s.Coins == 149 && creature.Level == 4 && creature.FormIndex == 0,
            "Evolução sem saldo não pode mudar forma, nível ou moedas.");
        await Dispose(main);
    }
    private async Task CheckMovement()
    {
        var main = Create(); var s = Session(main);
        s.StartingCoins = 1000; s.Restart();
        s.SelectSlot(Slot(main, 2)); Require(s.TrySummon("bulbasaur"), "Teste precisa de unidade inicial.");
        for (int level = 1; level < 4; level++) Require(s.TryUpgrade(), "Teste precisa evoluir antes de mover.");
        Require(s.TryEvolve(), "Teste deve comprar evolução explicitamente.");
        var first = Slot(main, 2).Occupant!;
        float range = first.Range, interval = first.AttackInterval;
        string name = first.DisplayName;
        Require(first.Level == 4 && first.FormIndex == 1, "Teste deve iniciar com unidade evoluída.");
        s.SelectSlot(Slot(main, 8)); s.TrySummon("charmander"); var second = Slot(main, 8).Occupant!; int balance = s.Coins;
        Require(s.CanReposition && s.BeginReposition(), "Mover deve ser permitido antes da primeira onda.");
        s.SelectSlot(Slot(main, 2)); Require(s.MoveSource == Slot(main, 2), "Clique ocupado deve escolher fonte.");
        Require(!s.TryUpgrade() && !s.TryEvolve() && s.Coins == balance,
            "Modo de reposicionamento deve bloquear compras.");
        s.SelectSlot(Slot(main, 5));
        Require(!Slot(main, 2).IsOccupied && Slot(main, 5).Occupant == first && !s.IsRepositioning && s.MoveSource == null
            && first.GlobalPosition == Slot(main, 5).GlobalPosition && first.Level == 4 && first.FormIndex == 1
            && first.Range == range && first.AttackInterval == interval && first.DisplayName == name
            && s.Coins == balance, "Mover deve preservar unidade evoluída, atributos e moedas.");
        s.BeginReposition(); s.SelectSlot(Slot(main, 5)); s.SelectSlot(Slot(main, 8));
        Require(Slot(main, 8).Occupant == first && Slot(main, 5).Occupant == second
            && first.GlobalPosition == Slot(main, 8).GlobalPosition && second.GlobalPosition == Slot(main, 5).GlobalPosition
            && first.Level == 4 && first.FormIndex == 1 && first.Range == range && first.AttackInterval == interval
            && first.DisplayName == name, "Troca deve preservar ambas as unidades e a forma evoluída.");
        s.BeginReposition(); s.SelectSlot(Slot(main, 8)); s.CancelReposition();
        Require(!s.IsRepositioning && s.MoveSource == null, "Cancelar deve limpar modo e fonte.");
        s.BeginReposition(); s.SelectSlot(Slot(main, 8)); s.TryStartWave();
        Require(!s.IsRepositioning && s.MoveSource == null && !s.CanReposition && !s.BeginReposition(), "Onda deve limpar e bloquear movimento.");
        s.SelectSlot(Slot(main, 8)); s.SelectSlot(Slot(main, 1));
        Require(Slot(main, 8).Occupant == first && !Slot(main, 1).IsOccupied, "Selecionar durante combate não pode mover."); await Dispose(main);
    }
    private async Task CheckWaves()
    {
        var main = Create(); SmallWaves(main, true); var s = Session(main); var d = s.Director;
        for (int wave = 1; wave <= 10; wave++)
        {
            Require(s.TryStartWave() && !s.TryStartWave(), "Cada onda exige início manual único."); d._Process(100);
            var enemies = Enemies(main); Require(enemies.Length == (wave == 10 ? 2 : 1), "Composição incorreta.");
            if (wave == 10)
            {
                Require(d.ActiveBoss == enemies.Single(e => e.IsBoss), "Chefe ativo deve apontar para a instância viva.");
                enemies.Single(e => e.IsBoss).TakeDamage(float.MaxValue);
                Require(d.BossDefeated && s.State == GameSession.MatchState.Fighting && d.Remaining == 1, "Matar chefe antes do acompanhante não pode vencer.");
            }
            foreach (var enemy in enemies.Where(e => e.IsTargetable))
            {
                int coins = s.Coins, reward = enemy.Reward;
                enemy.TakeDamage(float.MaxValue); enemy.TakeDamage(float.MaxValue);
                enemy.EmitSignal(Enemy.SignalName.Defeated); enemy.EmitSignal(Enemy.SignalName.ReachedChest);
                Require(s.Coins == coins + reward && s.Lives == 5, "Resultado repetido não pode duplicar recompensa/chegada.");
            }
            Require(!d.IsRunning && d.Remaining == 0, "Onda deve encerrar após todos resultados.");
            if (wave < 10)
            {
                Require(s.State == GameSession.MatchState.Preparing && s.CanReposition, "Intervalo deve liberar movimento.");
                d._Process(100); Require(d.CurrentWave == wave, "Próxima onda não pode começar automaticamente.");
            }
        }
        Terminal(main, GameSession.MatchState.Victory); s.Restart();
        Require(s.Coins == 150 && s.Lives == 5 && d.CurrentWave == 0 && !d.BossDefeated && d.ActiveBoss == null
            && s.CanReposition && !s.IsRepositioning && s.MoveSource == null && s.SelectedSlot == null, "Reinício deve restaurar estado e limpar chefe/movimento."); await Dispose(main);
    }
    private async Task CheckFinalDeathAnimationAndReset()
    {
        var main = Create(); SmallWaves(main); var s = Session(main); var d = s.Director;
        Enemy FinishWithBoss()
        {
            Enemy boss = null!;
            for (int wave = 1; wave <= 10; wave++)
            {
                Require(s.TryStartWave(), "Cenário de animação deve iniciar cada onda.");
                d._Process(100); var enemy = Enemies(main).Single();
                if (wave == 10) boss = enemy;
                enemy.TakeDamage(float.MaxValue);
            }
            Require(boss.IsBoss && s.State == GameSession.MatchState.Victory && d.BossDefeated && d.Remaining == 0,
                "Última morte deve encerrar a partida sem aguardar animação.");
            Require(GodotObject.IsInstanceValid(boss) && !boss.IsTargetable && !boss.IsQueuedForDeletion(),
                "Chefe morto deve continuar presente para concluir animação após a vitória.");
            return boss;
        }
        var finishedBoss = FinishWithBoss();
        for (int frame = 0; frame < 180 && GodotObject.IsInstanceValid(finishedBoss); frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(!GodotObject.IsInstanceValid(finishedBoss), "Animação final deve terminar e remover o chefe em até 180 frames.");
        s.Restart(); var resetBoss = FinishWithBoss();
        s.Restart();
        Require(resetBoss.IsQueuedForDeletion() && d.CurrentWave == 0 && !d.BossDefeated,
            "Reinício deve limpar imediatamente efeitos de morte e estado do chefe.");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(!GodotObject.IsInstanceValid(resetBoss), "Reinício deve remover o nó do efeito de morte.");
        await Dispose(main);
    }
    private async Task CheckDefeatAndRestart()
    {
        var main = Create(); SmallWaves(main); var s = Session(main); var d = s.Director;
        for (int wave = 1; wave <= 10; wave++)
        {
            s.TryStartWave(); d._Process(100); var enemy = Enemies(main).Single();
            if (wave < 10) enemy.TakeDamage(float.MaxValue); else { enemy.ProgressRatio = 1; enemy._Process(0); }
        }
        Terminal(main, GameSession.MatchState.Defeat); Require(!d.BossDefeated, "Chefe que chegou não conta como derrotado.");
        s.StartingLives = 1; s.Restart(); s.TryStartWave(); d._Process(0); var oldEnemy = Enemies(main).Single(); int coins = s.Coins;
        oldEnemy.ProgressRatio = 1; oldEnemy._Process(0); oldEnemy.EmitSignal(Enemy.SignalName.Defeated);
        Require(s.Coins == coins && s.Lives == 0, "Chegada fatal não deve pagar moedas."); Terminal(main, GameSession.MatchState.Defeat);
        s.Restart(); oldEnemy.EmitSignal(Enemy.SignalName.Defeated); oldEnemy.EmitSignal(Enemy.SignalName.ReachedChest);
        Require(s.Coins == 150 && s.Lives == 1 && d.Remaining == 0, "Sinais antigos devem ser ignorados após reinício.");
        d.Waves = new Godot.Collections.Array<WaveDefinition> { d.Waves[0] }; s.Restart();
        Require(!string.IsNullOrEmpty(d.GetConfigurationError()) && !s.TryStartWave(), "Configuração sem dez ondas e chefe deve ser bloqueada."); await Dispose(main);
    }
    private async Task CheckCancellationWithPendingSpawns()
    {
        var main = Create(); SmallWaves(main); var s = Session(main); var d = s.Director;
        var type = d.Waves[0].Composition[0];
        d.Waves[0].Composition.Add(type); d.Waves[0].Composition.Add(type);
        s.StartingLives = 1; s.Restart(); s.TryStartWave(); d._Process(0); d._Process(d.Waves[0].SpawnInterval + .01);
        var enemies = Enemies(main);
        Require(enemies.Length == 2 && d.PendingSpawns == 1 && d.Remaining == 3, "Cenário precisa de vivos e pendentes.");
        enemies[0].ProgressRatio = 1; enemies[0]._Process(0);
        enemies[1].EmitSignal(Enemy.SignalName.Defeated); enemies[1].EmitSignal(Enemy.SignalName.ReachedChest); d._Process(100);
        Terminal(main, GameSession.MatchState.Defeat);
        Require(s.Coins == 150 && s.Lives == 0, "Derrota deve cancelar vivos/pendentes sem recompensas.");
        s.Restart(); s.TryStartWave(); d._Process(0); var old = Enemies(main).Single();
        s.SelectSlot(Slot(main, 2)); s.TrySummon("bulbasaur"); s.Restart();
        old.EmitSignal(Enemy.SignalName.Defeated); old.EmitSignal(Enemy.SignalName.ReachedChest); d._Process(100);
        Require(d.CurrentWave == 0 && d.Remaining == 0 && !d.IsRunning && Enemies(main).Length == 0
            && s.Coins == 150 && s.Lives == 1 && !Slot(main, 2).IsOccupied && !s.IsLineageOwned("bulbasaur"),
            "Reinício durante onda deve limpar unidades/linhagens/vivos/pendentes e ignorar sinais antigos.");
        await Dispose(main);
    }
    private static void Terminal(Main main, GameSession.MatchState state)
    {
        var s = Session(main); int coins = s.Coins;
        Require(s.State == state && !s.CanAct && !s.TrySummon() && !s.TryUpgrade() && !s.TryEvolve() && !s.TryStartWave() && !s.BeginReposition()
            && s.Coins == coins && !s.Director.IsRunning && s.Director.Remaining == 0 && Enemies(main).Length == 0
            && main.GetNode<Control>("HUD/Interface/Result").Visible, "Estado terminal deve bloquear ações e limpar combate.");
    }
    private void Click(Vector2 position)
    {
        var viewport = GetViewport(); viewport.PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        foreach (bool pressed in new[] { true, false }) viewport.PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = pressed }, true);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
