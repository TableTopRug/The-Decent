using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;

public partial class Player : CharacterBody2D
{
	
	public const float Speed = 2.3f;
	public const float MaxSpeed = 48.0f;
	public const float JumpVelocity = 150f;
	public const float Friction = .7f;
	public const float AirResistMult = .3f;
	public const int NumJumps = 1;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	// public static float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() / 3.5f;
	public static float gravity = 210;

	public enum MoveDir
	{
		LEFT, 
		RIGHT, 
		UP, 
		DOWN, 
		num_move_dir
	}

	private Vector2? mouseDown = null;
	private Vector2? mouseUp = null;
	private Node2D? safeHome = null;
	private Node2D TRAJ_LINE;
	private bool md = false;
	private bool safe = false;
	private bool onFloor = false, Ystopped = false;
	private bool isHurting = false, hurt = false;
	private int jumpd = 0;
	private int lives = 1;
	private int deaths = 0;
	Timer lifeTimer;


	[Signal]
	public delegate void HasEndedEventHandler(int code);


	public override void _Ready()
	{
		FloorBlockOnWall = true;
		FloorSnapLength = 0.5f;
		FloorStopOnSlope = true;
		MotionMode = MotionModeEnum.Grounded;


		lifeTimer = GetNode<Timer>("LifeTimer");
		Godot.Collections.Array<Godot.Node> nodes = GetTree().GetNodesInGroup("PlayerInteract");

		foreach (Node node in nodes) {
			if (node.IsClass("Area2D")) {
				if (((Area2D)node).Name == "Death") {
					GD.Print("Death linked");
					((Area2D)node).BodyEntered += OnGiveDeath;
				}
				if (((Area2D)node).Name == "Life") {
					GD.Print("Life Linked");
					((Area2D)node).BodyEntered += OnGiveLife;
				}
			}
		}

		GD.Print(gravity);

		base._Ready();
	}

	public override void _Process(double delta)
	{
		if (safe) {
			DrawTrajectory();
		} 
		if (TRAJ_LINE != null) {
			this.RemoveChild(TRAJ_LINE);
			TRAJ_LINE = null;
		}
		if (isHurting && hurt) {
			Visible = !Visible;
			hurt = !hurt;
		} else if (isHurting && !hurt) {
			hurt = !hurt;
		}

		base._Process(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.ButtonIndex == MouseButton.Left)
			{
				if (!md && eventMouseButton.Pressed)
				{
					mouseDown = eventMouseButton.GlobalPosition;
					md = true;
					return;
				}
				else if (md && !eventMouseButton.Pressed)
				{
					mouseUp = eventMouseButton.GlobalPosition;
					md = false;
					return;
				}
			}
		}
		else if (@event is InputEventKey eventKey)
		{
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
			{
				GetTree().Quit();
			}
			else
			{
			}
		}

		base._UnhandledInput(@event);
	}

	public override void _PhysicsProcess(double delta)
	{
		//get the velocity and transform
		Vector2 velocity = Velocity;
		Transform2D trans = GlobalTransform;
		Vector2 position = GlobalPosition;

		// check if attatched
		if (!safe) {
			
			//if it's not moving downward
			if (velocity.Y == 0 || velocity.Y == gravity) {
				jumpd = 0;
				// check to see if the y is changing
				if (Ystopped == true) {
					onFloor = true;
				} else {
					Ystopped = true;
				}
			} else {
				onFloor = false;
				Ystopped = false;
			}
			//if it's not on the floor
			if (!onFloor) {
				//add gravity
				velocity.Y += gravity * (float)delta - Friction * AirResistMult;
				
				//if it's moving
				if (Mathf.Abs(velocity.X) > 0) {
					//slow it down (friction)
					velocity.X = (float)Mathf.Lerp(velocity.X, 0, Friction * AirResistMult);
				}
			} else {
				//if it's moving
				if (Mathf.Abs(velocity.X) > 0) {
					//slow it down (friction)
					velocity.X = (float)Mathf.Lerp(velocity.X, 0, Friction);
				}

				velocity.Y = gravity;
			}
			
			//do input checking
			if (Input.IsActionPressed("left"))
			{
				velocity = move(velocity, MoveDir.LEFT);
			}
			if (Input.IsActionPressed("right"))
			{
				velocity = move(velocity, MoveDir.RIGHT);
			}
			if (Input.IsActionJustPressed("jump"))
			{
				velocity = Jump(velocity, 0);
			}
		} else {
			//this is input handeling
			Vector2 mp = GetGlobalMousePosition();
			float dlt = safeHome.GetAngleTo(mp);

			float tmpx = (Mathf.Cos(dlt) * 35) + safeHome.GlobalPosition.X;
			float tmpy = (Mathf.Sin(dlt) * 35) + safeHome.GlobalPosition.Y;


			trans.Origin.X = Mathf.MoveToward(GlobalPosition.X, tmpx, Speed/6);
			trans.Origin.Y = Mathf.MoveToward(GlobalPosition.Y, tmpy, Speed/6);

			// ((Sprite2D)FindChild("Sprite2D")).FlipV = false;
			// if  (mp.X > GlobalPosition.X) {
			// 	((Sprite2D)FindChild("Sprite2D")).FlipH = false;
			// } else {
			// 	// ((AnimatedSprite2D)FindChild("AnimatedSprite2D")).FlipH = true;
			// 	((Sprite2D)FindChild("Sprite2D")).FlipH = true;
			// }

			LookAt(mp);

			if (mouseDown != null && mouseUp != null && !md) {
				safe = false;
				Vector2 final = (Vector2)(mouseUp - mouseDown);


				GD.Print(final);
				if (Mathf.Abs(final.X) > 10) {
					final /= 10;

					velocity.X += final.X * Speed;
					velocity.Y += final.Y * Speed;

					mouseDown = mouseUp = null;
				}
			}
		}

		GlobalTransform = trans;
		Velocity = velocity;
		
		var collision = MoveAndCollide(Velocity * (float)delta);
		if (collision != null) {
			HandleCollision(collision);
		}
	}

	public void DrawTrajectory() 
	{
		Line2D line = new Line2D();
		Vector2 o = Position;

		
		line.AddPoint(o);
		o = new Vector2(o.X + Velocity.X, o.Y + Velocity.Y);
		line.AddPoint(o);
		o = new Vector2(o.X + Velocity.X, o.Y + Velocity.Y);
		line.AddPoint(o);
		o = new Vector2(o.X + Velocity.X, o.Y + Velocity.Y);
		line.AddPoint(o);
		o = new Vector2(o.X + Velocity.X, o.Y + Velocity.Y);
		line.AddPoint(o);

		
		// GD.Print(line.Points[0]);
		this.TRAJ_LINE = line;
		this.AddChild(line);
	}

	private void HandleCollision(KinematicCollision2D collision) 
	{
		Velocity = Velocity.Snapped(collision.GetNormal());
		Node collider = (Node)collision.GetCollider();

		if (collider.Name == "Spikes") {
			OnGiveDeath((Node2D)collider);
		}
		if (collider.HasSignal("FoundVessel")) {
			safeHome = (Node2D)collider;
			safe = true;
		} else {
			Velocity = Velocity.Slide(collision.GetNormal());
		}
	}

	private void OnGiveLife(Node2D body) 
	{
		if (body == null || !body.IsClass("CharacterBody2D")) {
			return;
		}
		if (lifeTimer.TimeLeft <= 0 || lifeTimer.IsStopped()) {
			lives += 1;
			lifeTimer.Start(3);
		}
	}

	private void OnGiveDeath(Node2D body) 
	{
		if (body == null || (!body.IsClass("CharacterBody2D") && !(body.Name == "Spikes"))) {
			return;
		}
		if (FindChild("DeathTimer*") == null || ((Timer)FindChild("DeathTimer*")).TimeLeft <= 0) {
			deaths += 1;
			isHurting = true;

			if (deaths > lives) {
				GD.Print("Ded");
				SetProcess(false);
				SetPhysicsProcess(false);
				Engine.PhysicsTicksPerSecond = 10;
				SetPhysicsProcess(true);
				EmitSignal(SignalName.HasEnded, 0);
			} else {
				Timer deathTimer = new Timer();
				deathTimer.Name = "DeathTimer" + deaths;
				deathTimer.OneShot = true;
				deathTimer.Timeout += OnDeathPainTimeout;
				this.AddChild(deathTimer);
				deathTimer.Start(2);
			}
		}
	}

	private void OnDeathPainTimeout() 
	{
		isHurting = false;
		hurt = true;
		Visible = true;
	}

	private Vector2 MoveLeft(Vector2 velocity, float delta)
	{
		if (velocity.X > -MaxSpeed && velocity.X - delta > -MaxSpeed)
		{
			velocity.X = Mathf.Lerp(velocity.X, -MaxSpeed, delta);
		}
		else
		{
			velocity.X = -MaxSpeed;
		}

		return velocity;
	}

	private Vector2 MoveRight(Vector2 velocity, float delta)
	{
		if (velocity.X < MaxSpeed && velocity.X + delta < MaxSpeed)
		{
			velocity.X = Mathf.Lerp(velocity.X, MaxSpeed, delta);
		}
		else
		{
			velocity.X = MaxSpeed;
		}

		return velocity;
	}

	private Vector2 Jump(Vector2 velocity, float delta)
	{
		if (NumJumps > jumpd)
		{
			velocity.Y -= JumpVelocity;
			jumpd++;
			onFloor = false;
			Ystopped = false;
		}

		return velocity;
	}

	public Vector2 move(Vector2 velocity, MoveDir dir)
	{
		float delta;

		if (!onFloor)
		{
			delta = Speed * AirResistMult;
		} else
		{
			delta = Speed - Friction;
		}
		
		switch (dir)
		{
			case MoveDir.LEFT:
				velocity = MoveLeft(velocity, delta); 
				break;
			case MoveDir.RIGHT:
				velocity = MoveRight(velocity, delta);
				break;
			case MoveDir.UP:
				velocity = Jump(velocity, 0);
				break;
			case MoveDir.DOWN:
				//TODO: MoveDown
				break;	
			default:
				break;
		}
		
		return velocity;
	}

}
