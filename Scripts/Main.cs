using Godot;
using System.Globalization;
using System.Linq;

public partial class Main : Node2D
{
    private GameSession _session = null!;
    private Label _stats = null!, _wave = null!, _details = null!, _notice = null!, _resultText = null!;
    private Button _action = null!, _start = null!, _move = null!;
    private Button _evolve = null!;
    private readonly Button[] _summons = new Button[3];
    private Control _result = null!, _bossPanel = null!;
    private Label _bossName = null!;
    private ProgressBar _bossHealth = null!;
    private AudioController _audio = null!;
    private static string Number(float value) => value.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));

    public override void _Ready()
    {
        _session = GetNode<GameSession>("GameSession");
        _audio = GetNode<AudioController>("/root/GameAudio");
        _audio.RestartPlayback();
        _session.SoundRequested += OnSound;
        _session.MatchFinished += OnMatchFinished;
        _session.Restarted += OnRestarted;
        _session.Director.EnemySpawned += OnEnemySpawned;
        _session.Director.EnemyResolved += OnEnemyResolved;
        _stats = GetNode<Label>("HUD/Interface/Top/Rows/Stats");
        _wave = GetNode<Label>("HUD/Interface/Top/Rows/Wave");
        _bossPanel = GetNode<Control>("HUD/Interface/Top/Rows/Boss");
        _bossName = _bossPanel.GetNode<Label>("Name");
        _bossHealth = _bossPanel.GetNode<ProgressBar>("Health");
        _details = GetNode<Label>("HUD/Interface/Bottom/Rows/Details");
        _notice = GetNode<Label>("HUD/Interface/Bottom/Rows/Notice");
        _action = GetNode<Button>("HUD/Interface/Bottom/Rows/Actions/SelectionAction");
        _start = GetNode<Button>("HUD/Interface/Bottom/Rows/Actions/StartWave");
        _move = GetNode<Button>("HUD/Interface/Bottom/Rows/Actions/Move");
        _evolve = GetNode<Button>("HUD/Interface/Bottom/Rows/Evolve");
        _evolve.Pressed += OnClick;
        _evolve.Pressed += () => _session.TryEvolve();
        _result = GetNode<Control>("HUD/Interface/Result");
        _resultText = GetNode<Label>("HUD/Interface/Result/Panel/Rows/ResultText");
        for (int i = 0; i < 3; i++)
        {
            var type = _session.Pokemon[i];
            var button = GetNode<Button>($"HUD/Interface/Bottom/Rows/Portraits/Summon{i}");
            _summons[i] = button;
            button.Pressed += OnClick;
            button.Icon = type.Portrait;
            button.Pressed += () => _session.TrySummon(type.Lineage);
        }
        _action.Pressed += OnClick;
        _start.Pressed += OnClick;
        _move.Pressed += OnClick;
        _action.Pressed += () => _session.TryUpgrade();
        _start.Pressed += () => _session.TryStartWave();
        _move.Pressed += () =>
        {
            if (_session.IsRepositioning) _session.CancelReposition();
            else _session.BeginReposition();
        };
        var restart = GetNode<Button>("HUD/Interface/Result/Panel/Rows/Restart");
        restart.Pressed += _session.Restart;
        restart.Pressed += OnClick;
        _session.Changed += Refresh;
        Refresh();
    }

    private void OnClick() => _audio.PlaySfx("ui_click");
    private void OnSound(string name) => _audio.PlaySfx(name);
    private void OnRestarted() => _audio.RestartPlayback();
    private void OnMatchFinished(bool victory) => _audio.PlayResult(victory);
    private void OnEnemySpawned(Enemy enemy)
    {
        enemy.HitReceived += () => _audio.PlaySfx("hit");
        if (enemy.IsBoss) _audio.PlaySfx("boss_alert");
    }
    private void OnEnemyResolved(Enemy enemy, bool arrived)
    {
        if (!arrived) _audio.PlaySfx("enemy_defeat");
    }

    public override void _ExitTree()
    {
        if (_session == null) return;
        _session.SoundRequested -= OnSound;
        _session.MatchFinished -= OnMatchFinished;
        _session.Restarted -= OnRestarted;
        _session.Director.EnemySpawned -= OnEnemySpawned;
        _session.Director.EnemyResolved -= OnEnemyResolved;
        _session.Changed -= Refresh;
    }

    public override void _Process(double delta)
    {
        var boss = _session.Director.ActiveBoss;
        _bossPanel.Visible = boss != null && GodotObject.IsInstanceValid(boss) && boss.IsTargetable;
        if (!_bossPanel.Visible) return;
        _bossName.Text = $"REI OGRO  {boss!.CurrentHealth:0}/{boss.MaxHealth:0}";
        _bossHealth.MaxValue = boss.MaxHealth;
        _bossHealth.Value = boss.CurrentHealth;
    }

    private void Refresh()
    {
        var director = _session.Director;
        _stats.Text = $"Moedas: {_session.Coins}     Vidas do baú: {_session.Lives}";
        _wave.Text = $"Onda {director.CurrentWave} / {director.WaveCount}   •   Inimigos pendentes: {director.Remaining}";
        _notice.Text = _session.Message;
        _start.Text = director.IsRunning ? "Em combate" : director.CurrentWave == 0 ? "Iniciar onda" : "Próxima onda";
        _start.Disabled = !_session.CanStartWave;
        _move.Text = _session.IsRepositioning ? "Cancelar" : "Reposicionar";
        _move.Disabled = !_session.CanReposition;
        var slot = _session.SelectedSlot;
        for (int i = 0; i < 3; i++)
        {
            var type = _session.Pokemon[i];
            bool owned = _session.IsLineageOwned(type.Lineage);
            _summons[i].Text = type.FormNames[0] + (owned ? "\nInvocado" : $"\n{_session.SummonCost} moedas");
            _summons[i].Disabled = owned || !_session.CanAct || _session.IsRepositioning
                || slot == null || slot.IsOccupied || _session.Coins < _session.SummonCost;
        }
        _action.Disabled = true;
        _evolve.Disabled = true;
        _evolve.Text = "Selecione um Pokémon para evoluir";
        _action.Text = "Melhorar";
        if (slot?.IsOccupied == true)
        {
            var unit = slot.Occupant!;
            bool maximum = unit.Level >= _session.MaxLevel;
            string evolution = "Forma final alcançada.";
            int next = unit.FormIndex + 1;
            if (next < unit.Definition.FormNames.Length)
            {
                int required = unit.Definition.EvolutionLevels[next];
                int cost = _session.NextEvolutionCost;
                evolution = $"Evolução: Nv. {required} · {cost} moedas · dano {Number(unit.Definition.CalculateDamage(unit.Level, next))}";
                _evolve.Text = $"Evoluir: {unit.Definition.FormNames[next]} · Nv. {required} · {cost} moedas";
                _evolve.Disabled = !_session.CanAct || _session.IsRepositioning || unit.Level < required || _session.Coins < cost;
            }
            else _evolve.Text = "Forma final alcançada";
            _details.Text = $"{unit.DisplayName} · Nv. {unit.Level}/{_session.MaxLevel} · Forma {unit.FormIndex + 1}\n"
                + $"Dano {Number(unit.Damage)}" + (maximum ? "" : $" → {Number(unit.DamageAtLevel(unit.Level + 1))}")
                + $"   •   {Number(unit.Range)} px   •   {Number(unit.AttackInterval)} s\n"
                + (maximum ? "Nível máximo" : $"Melhoria: {_session.NextUpgradeCost} moedas");
            _evolve.TooltipText = evolution;
            _action.Text = maximum ? "Nível máximo" : $"Melhorar · {_session.NextUpgradeCost}";
            _action.Disabled = !_session.CanAct || _session.IsRepositioning || maximum || _session.Coins < _session.NextUpgradeCost;
        }
        else _details.Text = slot == null ? "Selecione uma das nove bases.\nInvoque até três Pokémon únicos.\nMelhore e reposicione entre as ondas."
            : "Base vazia selecionada.\nEscolha um dos retratos acima.\nCada linhagem pode ser invocada uma única vez.";
        _result.Visible = !_session.CanAct;
        _resultText.Text = _session.State == GameSession.MatchState.Victory
            ? "Vitória!\nO Rei Ogro foi derrotado." : "Derrota!\nO baú ficou sem vidas.";
    }
}
