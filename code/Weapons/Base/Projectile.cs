﻿using Grubs.Player;
using Grubs.States;
using Grubs.Utils;

namespace Grubs.Weapons.Projectiles;

public class Projectile : ModelEntity, IResolvable
{
	public bool Resolved => false;

	private bool IsResolved { get; set; }

	private TimeSince TimeSinceSegmentStarted { get; set; }
	private float CollisionExplosionDelaySeconds { get; set; }
	private float Speed { get; set; } = 0.001f;
	private float ExplosionRadius { get; set; } = 100;
	private List<ArcSegment>? Segments { get; set; }
	private Grub? _Grub { get; set; }

	public Projectile WithGrub( Grub grub )
	{
		_Grub = grub;
		return this;
	}

	public Projectile WithModel( string modelPath )
	{
		SetModel( modelPath );
		return this;
	}

	public Projectile SetPosition( Vector3 position )
	{
		Position = position.WithY( 0f );
		return this;
	}

	public Projectile WithCollisionExplosionDelay( float delaySeconds )
	{
		CollisionExplosionDelaySeconds = delaySeconds;
		return this;
	}

	public Projectile WithExplosionRadius( float explosionRadius )
	{
		ExplosionRadius = explosionRadius;
		return this;
	}

	public Projectile WithSpeed( float speed )
	{
		Speed = 1 / speed;
		return this;
	}

	public Projectile MoveAlongTrace( List<ArcSegment> points )
	{
		Segments = points;

		// Set the initial position
		Position = Segments[0].StartPos;

		return this;
	}

	[Event.Tick]
	public void Tick()
	{
		// Don't move if the projectile is parented.
		if ( Parent.IsValid() )
			return;

		// This might be shite
		if ( Segments is null || !Segments.Any() )
			return;

		if ( IsResolved )
			return;

		DrawSegments();

		if ( (Segments[0].EndPos - Position).IsNearlyZero( 2.5f ) )
		{
			if ( Segments.Count > 1 )
				Segments.RemoveAt( 0 );
			else
			{
				OnCollision();
				return;
			}

			TimeSinceSegmentStarted = 0;
		}
		else
		{
			Rotation = Rotation.LookAt( Segments[0].EndPos - Segments[0].StartPos );
			Position = Vector3.Lerp( Segments[0].StartPos, Segments[0].EndPos, Time.Delta / Speed );
		}
	}

	public void OnCollision()
	{
		if ( !IsServer )
			return;

		if ( CollisionExplosionDelaySeconds > 0 )
		{
			ExplodeAfterSeconds( CollisionExplosionDelaySeconds );
			return;
		}

		Explode();
	}

	public async void ExplodeAfterSeconds( float seconds )
	{
		await GameTask.DelaySeconds( seconds );

		if ( !IsValid )
			return;

		Explode();
	}

	private void Explode()
	{
		if ( _Grub is null )
			Log.Warning( $"{this} is missing the {nameof( _Grub )} value. Fix this!" );

		ExplosionHelper.Explode( Position, _Grub!, ExplosionRadius );
		Delete();
	}

	private void DrawSegments()
	{
		if ( Segments is null )
			return;

		foreach ( var segment in Segments )
		{
			DebugOverlay.Line( segment.StartPos, segment.EndPos );
		}
	}
}