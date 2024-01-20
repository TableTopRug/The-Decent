using Godot;
using System;
using System.Linq;

public partial class MapStaticBody2D : StaticBody2D
{	

	[Signal]
	public delegate void MapCollidedEventHandler();
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Node[] children = GetChildren().ToArray();

		foreach (Node vnode in children)
		{
			((Node2D)vnode).AddUserSignal("MapCollided");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
