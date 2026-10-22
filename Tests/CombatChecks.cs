using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CombatChecks : Node
{
    public override async void _Ready()
    {
        try
        {
            await CheckTargetingAndLifecycle();
            await CheckMovementAndArrival();
            await CheckAnimationSpeedDoesNotChangeAttacks();
            GD.Print("CombatChecks: todos os testes passaram."); TestExit.Quit(this);
        }
        catch (Exception error) { GD.PushError(error.ToString()); TestExit.Quit(this, 1); }
    }
    private Main Create()
    {
        var main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>(); AddChild(main);
        main.GetNode<GameSession>("GameSession").Director.SetProcess(false); return main;
    }
    private static Enemy Spawn(Main main, float progress)
    {
        var definition = main.GetNode<GameSession>("GameSession").Director.Waves.SelectMany(w => w.Composition).First(e => !e.IsBoss);
        var enemy = GD.Load<PackedScene>("res://Scenes/Enemy.tscn").Instantiate<Enemy>();
        enemy.Configure(definition, 10); main.GetNode<Path2D>("EnemyPath").AddChild(enemy);
        enemy.SetProcess(false); enemy.Progress = progress; return enemy;
    }
    private async Task CheckTargetingAndLifecycle()
    {
        var main = Create(); var session = main.GetNode<GameSession>("GameSession");
        var slot = main.GetNode<CreatureSlot>("Slots/Slot2"); session.SelectSlot(slot);
        Require(session.TrySummon("bulbasaur"), "Teste precisa de defensor real.");
        var creature = slot.Occupant!; creature.SetProcess(false);
        var first = Spawn(main, 100); var second = Spawn(main, 120);
        // Put both enemies within the defender's real range without changing its definition.
        creature.GlobalPosition = second.GlobalPosition;
        float firstHealth = first.CurrentHealth, secondHealth = second.CurrentHealth, damage = creature.Damage;
        creature._Process(0);
        Require(first.CurrentHealth == firstHealth && second.CurrentHealth == secondHealth
            && GetTree().GetNodesInGroup("elemental_attacks").OfType<ElementalAttack>().Any(a => !a.IsResolved),
            "Lançamento deve criar efeito sem causar dano antes do impacto.");
        ResolveAttacks();
        Require(first.CurrentHealth == firstHealth && Mathf.IsEqualApprox(second.CurrentHealth, secondHealth - damage), "Alvo mais avançado deve receber dano real.");
        Require(second.GetNode<ProgressBar>("HealthBar").Value == second.CurrentHealth,
            "Impacto deve atualizar barra de vida.");
        creature._Process(creature.AttackInterval / 2);
        Require(Mathf.IsEqualApprox(second.CurrentHealth, secondHealth - damage), "Intervalo deve bloquear ataque antecipado.");
        first.Progress = 130; creature._Process(creature.AttackInterval / 2 + .01);
        ResolveAttacks();
        Require(Mathf.IsEqualApprox(first.CurrentHealth, firstHealth - damage), "Prioridade deve acompanhar progresso.");
        // The most advanced enemy is out of range; the other is still colocated.
        first.GlobalPosition = creature.GlobalPosition + new Vector2(creature.Range + 20, 0);
        creature._Process(creature.AttackInterval + .01);
        ResolveAttacks();
        Require(Mathf.IsEqualApprox(second.CurrentHealth, secondHealth - 2 * damage), "Alvo fora do alcance deve ser substituído.");
        int defeats = 0, arrivals = 0; first.Defeated += () => defeats++; first.ReachedChest += () => arrivals++;
        float health = first.CurrentHealth; first.TakeDamage(0); first.TakeDamage(-10);
        Require(first.CurrentHealth == health, "Dano inválido não pode ferir nem curar.");
        first.TakeDamage(float.MaxValue); first.TakeDamage(float.MaxValue); first.ProgressRatio = 1; first._Process(0);
        Require(first.CurrentHealth == 0 && !first.IsTargetable && defeats == 1 && arrivals == 0
            && GodotObject.IsInstanceValid(first) && !first.IsQueuedForDeletion(), "Morte deve emitir uma vez e preservar nó durante animação.");
        second.Cancel(); creature._Process(creature.AttackInterval + .01);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        creature._Process(creature.AttackInterval + .01);
        var replacement = Spawn(main, 120); float replacementHealth = replacement.CurrentHealth;
        creature._Process(0);
        ResolveAttacks();
        Require(Mathf.IsEqualApprox(replacement.CurrentHealth, replacementHealth - damage), "Defensor deve readquirir alvo após remoções.");
        creature.SetCombatEnabled(false); creature._Process(100);
        ResolveAttacks();
        Require(Mathf.IsEqualApprox(replacement.CurrentHealth, replacementHealth - damage)
            && !GetTree().GetNodesInGroup("elemental_attacks").OfType<ElementalAttack>().Any(a => !a.IsResolved),
            "Desativar combate deve parar novos ataques e dano.");
        // Wait for production animation rather than freeing the defeated node ourselves.
        for (int frame = 0; frame < 180 && GodotObject.IsInstanceValid(first); frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(!GodotObject.IsInstanceValid(first), "Animação de morte deve remover nó em tempo limitado.");
        session.Restart(); main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task CheckMovementAndArrival()
    {
        var main = Create(); var enemy = Spawn(main, 0); int arrivals = 0, defeats = 0;
        enemy.ReachedChest += () => arrivals++; enemy.Defeated += () => defeats++;
        enemy._Process(.5);
        Require(Mathf.IsEqualApprox(enemy.Progress, enemy.Speed * .5f), "Movimento deve usar velocidade e delta reais.");
        float health = enemy.CurrentHealth; enemy.ProgressRatio = 1; enemy._Process(0); enemy.TakeDamage(float.MaxValue); enemy._Process(1);
        Require(!enemy.IsTargetable && enemy.CurrentHealth == health && arrivals == 1 && defeats == 0, "Chegada deve ocorrer uma vez e impedir morte posterior.");
        var cancelled = Spawn(main, 100); int cancelledResults = 0;
        cancelled.Defeated += () => cancelledResults++; cancelled.ReachedChest += () => cancelledResults++;
        cancelled.Cancel(); cancelled.TakeDamage(float.MaxValue); cancelled.ProgressRatio = 1; cancelled._Process(0);
        Require(!cancelled.IsTargetable && cancelled.IsQueuedForDeletion() && cancelledResults == 0, "Cancelar deve remover sem resultado de combate.");
        main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task CheckAnimationSpeedDoesNotChangeAttacks()
    {
        float? baselineDamage = null;
        foreach (double animationFps in new[] { .5, 120.0 })
        {
            var main = Create(); var session = main.GetNode<GameSession>("GameSession");
            var source = session.Pokemon.First(type => type.Lineage == "bulbasaur");
            var originalSpeeds = source.Frames.GetAnimationNames().ToDictionary(name => name,
                name => source.Frames.GetAnimationSpeed(name));
            var definition = (PokemonDefinition)source.Duplicate();
            definition.Frames = (SpriteFrames)source.Frames.Duplicate(true);
            foreach (var animation in definition.Frames.GetAnimationNames())
                definition.Frames.SetAnimationSpeed(animation, animationFps);
            Require(definition != source && definition.Frames != source.Frames,
                "Teste deve usar definição e SpriteFrames privados.");
            var creature = GD.Load<PackedScene>("res://Scenes/Creature.tscn").Instantiate<Creature>();
            creature.Configure(definition); main.AddChild(creature); creature.SetProcess(false);
            var enemy = Spawn(main, 120); creature.GlobalPosition = enemy.GlobalPosition;
            float initialHealth = enemy.CurrentHealth;
            int attacks = 0;
            // Let real sprite animation advance while gameplay receives identical controlled deltas.
            for (int frame = 0; frame < 180; frame++)
            {
                float before = enemy.CurrentHealth;
                creature._Process(1.0 / 60);
                ResolveAttacks();
                float lost = before - enemy.CurrentHealth;
                Require(lost == 0 || Mathf.IsEqualApprox(lost, creature.Damage),
                    "Um ataque deve aplicar exatamente uma parcela de dano.");
                if (lost > 0) attacks++;
                float after = enemy.CurrentHealth;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(enemy.CurrentHealth == after,
                    "Frames e término de animação não podem aplicar dano adicional.");
            }
            float totalDamage = initialHealth - enemy.CurrentHealth;
            Require(attacks == 3 && Mathf.IsEqualApprox(totalDamage, 3 * creature.Damage),
                "Três segundos devem produzir três ataques pelo intervalo real, independentemente do FPS visual.");
            Require(baselineDamage == null || Mathf.IsEqualApprox(totalDamage, baselineDamage.Value),
                "Alterar FPS visual não pode alterar número ou dano dos ataques.");
            baselineDamage = totalDamage;
            foreach (var speed in originalSpeeds)
                Require(source.Frames.GetAnimationSpeed(speed.Key) == speed.Value,
                    "Teste não pode modificar SpriteFrames compartilhados.");
            session.Restart(); main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
    private void ResolveAttacks()
    {
        // Advance production effects deterministically; rendering/animation callbacks are not damage clocks.
        foreach (var attack in GetTree().GetNodesInGroup("elemental_attacks").OfType<ElementalAttack>())
        {
            attack.SetProcess(false);
            if (!attack.IsResolved) attack._Process(1);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
