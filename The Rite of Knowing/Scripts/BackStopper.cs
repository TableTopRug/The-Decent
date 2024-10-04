using Godot;
using System;


public partial class BackStopper : AnimatableBody2D
{	
	CollisionPolygon2D trigger;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Godot.Collections.Array<Node> nodes = GetTree().GetNodesInGroup("PlayerInteract");

		foreach (Node node in nodes) {
			if (node.IsClass("CollisionShape2D")) {
				if (((CollisionShape2D)node).Name == "Section 1 Boundary") {
					GD.Print("Found Backstop Trigger");
					((Area2D)node.GetParent()).BodyEntered += OnShapeEntered;
					trigger = (CollisionPolygon2D)node;
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void OnShapeEntered(Node2D body) {
		GD.Print("trigger rock fall");
		Fall();
		trigger.Disabled = true;
	}

	private void Fall() {
		
	}
}
