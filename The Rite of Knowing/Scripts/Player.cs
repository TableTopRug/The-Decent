using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export]
	public const float Speed = 10.0f;
	[Export]
	public const float JumpVelocity = 159f;
	[Export]
	public const float Friction = .01f;
	[Export]
	public const float AirResistMult = .5f;
	[Export]
	public const int NumJumps = 1;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private Vector2? mouseDown = null;
	private Vector2? mouseUp = null;
	private bool md = false;
	private bool safe = false;
	private bool onFloor = false;
	private bool isHurting = false, hurt = false;
	private Node2D? safeHome = null;
	private Node2D TRAJ_LINE;
	private int jumpd = 0;
	private int lives = 1;
	Timer lifeTimer;
	private int deaths = 0;


	[Signal]
	public delegate void HasEndedEventHandler(int code);


	public override void _Ready()
	{
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

		base._Ready();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton) {
			if (eventMouseButton.ButtonIndex == MouseButton.Left) {
				if (!md && eventMouseButton.Pressed) {
					mouseDown = eventMouseButton.GlobalPosition;
					md = true;
					// GD.Print("pressed");
					return;
				} else if (md && !eventMouseButton.Pressed) {
					mouseUp = eventMouseButton.GlobalPosition;
					md = false;
					// GD.Print("released");
					return;
				}
			}
		}

		base._UnhandledInput(@event);
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

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		Transform2D trans = GlobalTransform;

		// Add the gravity.
		if (!safe) {
			// GD.Print(onFloor);
			if (velocity.Y == 0) {
				jumpd = 0;
			}
			if (!onFloor) {
				if (Input.IsActionPressed("left")) {
				velocity.X -= Speed * AirResistMult;
				}
				if (Input.IsActionPressed("right")) {
					velocity.X += Speed * AirResistMult;
				}
				velocity.Y += gravity * (float)delta + Friction * AirResistMult;
				
				if (velocity.X > 0) {
					velocity.X = (float)Mathf.Lerp(velocity.X, 0, Friction * AirResistMult);
				}
			} else {
				if (Input.IsActionPressed("left")) {
				velocity.X -= Speed;
				}
				if (Input.IsActionPressed("right")) {
					velocity.X += Speed;
				}
				if (velocity.X > 0) {
					velocity.X = (float)Mathf.Lerp(velocity.X, 0, Friction);
				}
				
				velocity.Y = 0;
				onFloor = true;
			}
			if (onFloor) {
				jumpd = 0;
			}
			if (Input.IsActionJustPressed("jump") && NumJumps > jumpd) {
				velocity.Y -= JumpVelocity;
				jumpd++;
				onFloor = false;
			}
		} else {
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


				// GD.Print(final);
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

		if (IsOnFloor() || IsOnWall()) {
			jumpd = 0;
		}
	}

	public void DrawTrajectory() {
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

	private void HandleCollision(KinematicCollision2D collision) {
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

	private void OnGiveLife(Node2D body) {
		if (body == null || !body.IsClass("CharacterBody2D")) {
			return;
		}
		if (lifeTimer.TimeLeft <= 0 || lifeTimer.IsStopped()) {
			lives += 1;
			lifeTimer.Start(3);
		}
	}

	private void OnGiveDeath(Node2D body) {
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

	private void OnDeathPainTimeout() {
		isHurting = false;
		hurt = true;
		Visible = true;
	}

}
