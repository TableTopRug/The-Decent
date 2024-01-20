using Godot;
using System;

public partial class Movement : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 300.0f;
	[Export]
	public float RotationSpeed { get; set; } = 1.5f;

	private float _rotationDirection;


	public void GetInput()
	{

		// Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		// Velocity = inputDirection * Speed;

		// _rotationDirection = Input.GetAxis("left", "right");
		// Velocity = Transform.X * Input.GetAxis("down", "up") * Speed;

		LookAt(GetGlobalMousePosition()); // same as var rotation = GetGlobalMousePosition().AngleToPoint(Position);
        Velocity = Transform.X * Input.GetAxis("down", "up") * Speed;
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		// Rotation += _rotationDirection * RotationSpeed * (float)delta;
		MoveAndSlide();
	}


}
