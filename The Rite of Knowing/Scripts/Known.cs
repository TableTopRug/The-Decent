using Godot;
using System;

public partial class Known : StaticBody2D
{
	[Signal]
	public delegate void FoundVesselEventHandler();


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddToGroup("PlayerInteract");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
