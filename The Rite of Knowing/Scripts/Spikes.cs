using Godot;
using System;

public partial class Spikes : StaticBody2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Draw()
	{
		foreach(Node2D child in this.GetChildren()) {
			// RenderingServer.CanvasItemSetSelfModulate(child.GetCanvasItem(), new Color(0.0f, 0.0f, 0.0f, 0.0f));
			RenderingServer.CanvasItemSetSelfModulate(child.GetCanvasItem(), new Color("ff00ff"));
		}
		base._Draw();
	}
}
