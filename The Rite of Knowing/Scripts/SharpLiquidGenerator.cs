using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SharpLiquidGenerator : Node2D
{
	[ExportGroup("Modifiable Constants")]
	[Export]
	public Texture2D PARTICLE_TEXTURE;
	[Export]
	public int MAX_LIQUID_PARTICLES = 1000;
	[Export]
	public float SPAWN_TIME = 1.0f;
	[Export]
	public float SPAWN_RAD = 0.5f;
	[Export]
	public uint LIQUID_COLLISION_LAYER = 1;
	[Export]
	public int PARTICLE_SIZE = 8;
	[Export]
	public float PARTICLE_FRICTION = 0.0f;
	[Export]
	public float PARTICLE_MASS = 0.05f;
	[Export]
	public float PARTICLE_GRAVITY = 10.0f;

	private int curParticleCount = 0;
	private float spawnTimer = 0;
	private List<(Rid, Rid)> particles = new List<(Rid, Rid)>();


	public void CreateParticle() {		
		var trans = GlobalTransform;
		trans.Translated(new Vector2((float)GD.RandRange(-SPAWN_RAD, SPAWN_RAD), (float)GD.RandRange(-SPAWN_RAD, SPAWN_RAD)));

		// Create Physics body
		Rid liquidBody = PhysicsServer2D.BodyCreate();
		PhysicsServer2D.BodySetMode(liquidBody, PhysicsServer2D.BodyMode.Rigid);
		PhysicsServer2D.BodySetSpace(liquidBody, GetWorld2D().Space);

		// create circle shape
		Rid shape = PhysicsServer2D.CircleShapeCreate();
		PhysicsServer2D.ShapeSetData(shape, PARTICLE_SIZE);

		// add shape to rigidbody
		PhysicsServer2D.BodyAddShape(liquidBody, shape);

		// collissions
		PhysicsServer2D.BodySetCollisionLayer(liquidBody, LIQUID_COLLISION_LAYER);
		PhysicsServer2D.BodySetCollisionMask(liquidBody, LIQUID_COLLISION_LAYER);

		// physics params 
		PhysicsServer2D.BodySetParam(liquidBody, PhysicsServer2D.BodyParameter.Friction, PARTICLE_FRICTION);
		PhysicsServer2D.BodySetParam(liquidBody, PhysicsServer2D.BodyParameter.Mass, PARTICLE_MASS);
		PhysicsServer2D.BodySetParam(liquidBody, PhysicsServer2D.BodyParameter.GravityScale, PARTICLE_GRAVITY);
		PhysicsServer2D.BodySetState(liquidBody, PhysicsServer2D.BodyState.Transform, trans);

		// create canvas item
		Rid liquidParticle = RenderingServer.CanvasItemCreate();

		//set parent
		RenderingServer.CanvasItemSetParent(liquidParticle, GetCanvasItem());

		// set transform
		RenderingServer.CanvasItemSetTransform(liquidParticle, trans);

		//create texture rect
		Rect2 rect = new Rect2
		{
			Position = new Vector2(-PARTICLE_SIZE, -PARTICLE_SIZE),
			Size = PARTICLE_TEXTURE.GetSize() / 2
		};

		// add texture to canvas item
		RenderingServer.CanvasItemAddTextureRect(liquidParticle, rect, PARTICLE_TEXTURE._GetRid());

		// set tex color to pink (change in shader) [Pink]
		RenderingServer.CanvasItemSetSelfModulate(liquidParticle, new Color("ff00ff"));

		// add to array
		particles.Add((liquidBody, liquidParticle));
		GD.Print(particles.Count);
	}
	
	public override void _PhysicsProcess(double delta) {
		//add Particles if num < max
		if (spawnTimer < 0 && curParticleCount < MAX_LIQUID_PARTICLES) {
			CreateParticle();
			curParticleCount++;

			spawnTimer = SPAWN_TIME;
		} 
		
		spawnTimer -= 1;

		// update particle textures
		foreach (var particle in particles) {
			Transform2D trans = PhysicsServer2D.BodyGetState(particle.Item1, PhysicsServer2D.BodyState.Transform).AsTransform2D();
			trans.Origin -= GlobalTransform.Origin;
			RenderingServer.CanvasItemSetTransform(particle.Item2, trans);

			// delete it if we can't see it
			if (trans.Origin.Y > 1500) {	//down is positive
				//remove Rids
				PhysicsServer2D.FreeRid(particle.Item1);
				RenderingServer.FreeRid(particle.Item2);

				//remvoe form list
				particles.Remove(particle);
			}
		}
	}
}
