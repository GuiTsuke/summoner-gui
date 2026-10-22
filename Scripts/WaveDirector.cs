using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class WaveDirector : Node
{
    [Export] public PackedScene EnemyScene { get; set; } = null!;
    [Export] public Path2D EnemyPath { get; set; } = null!;
    [Export] public Godot.Collections.Array<WaveDefinition> Waves { get; set; } = new();
    public event Action? Changed;
    public event Action<Enemy>? EnemySpawned;
    public event Action<Enemy, bool>? EnemyResolved;
    public event Action? WaveCompleted;
    public int CurrentWave { get; private set; }
    public int WaveCount => Waves.Count;
    public int PendingSpawns { get; private set; }
    public int Remaining => PendingSpawns + _active.Count;
    public bool IsRunning { get; private set; }
    public bool IsStopped { get; private set; }
    public bool BossDefeated { get; private set; }
    public Enemy? ActiveBoss => _active.FirstOrDefault(enemy => enemy.IsBoss && enemy.IsTargetable);
    private readonly HashSet<Enemy> _active = new();
    private double _spawnTimeLeft;
    private int _nextIndex;

    public string GetConfigurationError()
    {
        if (Waves.Count != 10) return "Configure exatamente dez ondas.";
        int bosses = 0;
        for (int w = 0; w < Waves.Count; w++)
        {
            var wave = Waves[w];
            if (wave == null || wave.EnemyCount == 0 || wave.SpawnInterval < 0.05f || wave.HealthMultiplier <= 0)
                return $"Configuração inválida da onda {w + 1}.";
            for (int i = 0; i < wave.EnemyCount; i++)
            {
                var type = wave.Composition[i];
                if (type == null) return "Tipo de inimigo ausente.";
                if (type.IsBoss)
                {
                    bosses++;
                    if (w != 9 || i != wave.EnemyCount - 1) return "O chefe precisa ser o último surgimento da onda 10.";
                }
            }
        }
        return bosses == 1 ? "" : "Configure exatamente um chefe.";
    }

    public bool TryStartNextWave()
    {
        if (IsStopped || IsRunning || CurrentWave >= WaveCount) return false;
        if (CurrentWave == 0 && GetConfigurationError() != "") return false;
        PendingSpawns = Waves[CurrentWave].EnemyCount;
        CurrentWave++;
        _nextIndex = 0;
        IsRunning = true;
        _spawnTimeLeft = 0;
        Changed?.Invoke();
        return true;
    }

    public override void _Process(double delta)
    {
        if (!IsRunning || PendingSpawns == 0) return;
        _spawnTimeLeft -= delta;
        while (IsRunning && PendingSpawns > 0 && _spawnTimeLeft <= 0)
        {
            SpawnEnemy();
            _spawnTimeLeft += Waves[CurrentWave - 1].SpawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        var wave = Waves[CurrentWave - 1];
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Configure(wave.Composition[_nextIndex++], wave.HealthMultiplier);
        enemy.Loop = false;
        enemy.Defeated += () => Resolve(enemy, false);
        enemy.ReachedChest += () => Resolve(enemy, true);
        PendingSpawns--;
        _active.Add(enemy);
        EnemyPath.AddChild(enemy);
        EnemySpawned?.Invoke(enemy);
        Changed?.Invoke();
    }

    private void Resolve(Enemy enemy, bool arrived)
    {
        if (!_active.Remove(enemy)) return;
        if (enemy.IsBoss && !arrived) BossDefeated = true;
        EnemyResolved?.Invoke(enemy, arrived);
        if (IsRunning && Remaining == 0)
        {
            IsRunning = false;
            WaveCompleted?.Invoke();
        }
        Changed?.Invoke();
    }

    public void Stop(bool clearEffects = false)
    {
        IsStopped = true;
        IsRunning = false;
        PendingSpawns = 0;
        // Resolved death effects can finish at game over; reset removes them all.
        foreach (var node in EnemyPath.GetChildren())
            if (node is Enemy enemy && GodotObject.IsInstanceValid(enemy)
                && (clearEffects || enemy.IsTargetable)) enemy.Cancel();
        _active.Clear();
    }

    public void Reset()
    {
        Stop(clearEffects: true);
        CurrentWave = 0;
        IsStopped = false;
        BossDefeated = false;
        _spawnTimeLeft = 0;
        Changed?.Invoke();
    }
}
