using Godot;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        var enemy = GetNode<Enemy>("EnemyPath/Enemy");
        var status = GetNode<Label>("Status");

        enemy.ReachedChest += () => status.Text = "Inimigo chegou ao baú";
    }
}
