namespace Sandbox;

/// <summary>
/// Two-bone inverse kinematics
/// </summary>
public sealed class IKConstraint : BaseConstraint, Component.ExecuteInEditor
{
	/// <summary>
	/// Last of a chain of 3 bones (e.g. the hand in upperarm -> lowerarm -> hand)
	/// <br></br>
	/// The transform of this object will be set to the constraint components transform
	/// </summary>
	[Property] public GameObject EndBone { get; set; }
	[Property, Hide] public GameObject[] Bones { get; set; }
	[Property, Hide] public Transform[] Defaults { get; set; }

	/// <summary>
	/// Target must be part of a chain with at least 2 parent bones above it in the heirarchy
	/// </summary>
	public static bool IsTargetValid( GameObject o ) => o?.Parent?.Parent?.Parent is not null;

	/// <summary>
	/// Creates an IK constraint for the given bone
	/// </summary>
	public static IKConstraint CreateForTarget( GameObject o ) {
		if ( !IsTargetValid( o ) )
			return null;
		var c = new GameObject( o.Parent.Parent.Parent, true, o.Name ).Components.Create<IKConstraint>();
		c.WorldTransform = o.WorldTransform;
		c.EndBone = o;
		return c;
	}

	protected override void OnEnabled()
	{
		if ( !IsTargetValid( EndBone ) )
			return;
		Capture();
	}

	private void Capture()
	{
		Bones = [EndBone.Parent.Parent, EndBone.Parent, EndBone];
		Defaults = new Transform[3];
		var r = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInAncestors );
		for ( var i = 0; i < 3; i++ )
		{
			var go = Bones[i];
			Controls( go );
			if ( r.IsValid() )
			{
				var bone = r.Model.Bones.GetBone( go.Name );
				Vector3 offset;
				if ( bone is null || bone.Parent is null )
					offset = go.LocalTransform.Position;
				else
					offset = bone.Parent.LocalTransform.PointToLocal( bone.LocalTransform.Position );
				Defaults[i] = new( offset, go.LocalRotation );
			}
			else
				Defaults[i] = go.LocalTransform;
		}
	}

	protected override void OnUpdate()
	{
		if ( !IsTargetValid( EndBone ) )
			return;
		if ( Bones is null )
			Capture();

		var t0 = Bones[0].WorldTransform.WithRotation( Defaults[0].Rotation );
		var t1 = t0.ToWorld( Defaults[1] );
		var t2 = t1.ToWorld( Defaults[2] );
		var len1 = Defaults[1].Position.Length;
		var len2 = Defaults[2].Position.Length;
		var mlen = len1 + len2 - 0.01f;

		var end = (t2.Position - t0.Position).Normal;
		var pole = Vector3.VectorPlaneProject( t1.Position - t0.Position, end ).Normal;

		var targetDelta = GameObject.WorldPosition - t0.Position;
		if ( targetDelta.LengthSquared > mlen * mlen )
			targetDelta = targetDelta.Normal * mlen;
		var targetDeltaLen = targetDelta.Length;
		var target = targetDelta / targetDeltaLen;

		var cos = 0.0f;
		var denom = 2.0f * len1 * targetDeltaLen;
		if ( denom > 0.001f )
			cos = (targetDeltaLen * targetDeltaLen + len1 * len1 - len2 * len2) / denom;
		var poleDist = len1 * MathF.Sin( MathF.Acos( cos ) );

		var poleDir = Rotation.FromToRotation( end, target ) * pole;
		var outP1 = t0.Position + len1 * cos * target + poleDist * poleDir;
		var outR0 = Rotation.FromToRotation( t1.Position - t0.Position, outP1 - t0.Position );

		var endDir = t0.Position + outR0 * (t2.Position - t0.Position) - outP1;
		var outR1 = Rotation.FromToRotation( endDir, t0.Position + targetDelta - outP1 );

		Bones[0].WorldRotation = outR0 * t0.Rotation;
		Bones[1].WorldTransform = new( outP1, outR1 * Bones[0].WorldRotation * Defaults[1].Rotation );
		Bones[2].WorldTransform = WorldTransform;
	}

	protected override void DrawGizmos() {
		Gizmo.Hitbox.DepthBias *= 0.1f;
		Gizmo.Draw.LineThickness = 1;
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = Gizmo.IsSelected ? Gizmo.Colors.Active : Gizmo.IsHovered ? Color.White : Color.White.Darken( 0.5f );

		var dist = Gizmo.CameraTransform.Position.Distance( WorldPosition );
		var size = dist.Remap( 0, 256, 1.5f, 0.4f );
		if ( Gizmo.Camera.Ortho )
			size *= Gizmo.Camera.OrthoHeight * 0.006f;
		else
		{
			size *= 1024.0f / Gizmo.Camera.Size.Length;
			size *= dist * Gizmo.Camera.FieldOfView.DegreeToRadian() * 0.006f;
		}
		var rot = Gizmo.LocalCameraTransform.Rotation;

		Gizmo.Hitbox.Sphere( new Sphere( 0, size ) );
		Gizmo.Draw.SolidTriangle( rot.Left * size, rot.Up * size, rot.Right * size );
		Gizmo.Draw.SolidTriangle( rot.Left * size, rot.Down * size, rot.Right * size );

		if ( Gizmo.IsHovered && !Gizmo.IsSelected )
		{
			var offset = Gizmo.LocalCameraTransform.Position.Length.Remap( 0, 256, 12, 6 );
			Gizmo.Draw.ScreenText( GameObject.Name, WorldPosition, Vector2.Right * offset, size: 14, flags: TextFlag.LeftCenter );
		}
	}

	protected override void OnDisabled()
	{
		Defaults = null;
		Bones = null;
	}
}
