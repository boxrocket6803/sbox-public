namespace Sandbox;

/// <summary>
/// The base class for GameObject constraints.
/// </summary>
public abstract class BaseConstraint : Component
{
	private List<GameObject> _registered = [];

	protected void Controls( GameObject go )
	{
		if ( go.Flags.HasFlag( GameObjectFlags.Bone ) )
			go.Flags = go.Flags.WithFlag( GameObjectFlags.ProceduralBone, true );
		go.Constraints ??= [];
		go.Constraints.Add( this );
		_registered.Add( go );
	}

	internal override void OnDisabledInternal()
	{
		foreach ( var go in _registered )
			go.Constraints?.Remove( this );
		base.OnDisabledInternal();
	}
}
