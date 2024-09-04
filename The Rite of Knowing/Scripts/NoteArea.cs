using Godot;
using System;

public partial class NoteArea : Area2D
{
	[Export]
	public float Destination;
	[Export]
	public float change = 10;

	private float start;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		start = GlobalPosition.Y;
		Destination = change - start;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Transform2D trans = GlobalTransform;

		trans.Origin.Y = (float) Mathf.MoveToward(GlobalPosition.Y, Destination, delta * 1.7);

		if (GlobalPosition.Y - Math.Abs(Destination) < .01) {
			Destination = start + change;
		}

		GlobalTransform = trans;
	}

	private void _on_body_entered(PhysicsBody2D body)
	{
		GD.Print("note get");
		QueueFree();
	}
}
