namespace Prp;

[PrpType(TypeIndex.SceneObject)]
public class SceneObject : PrpObject {
	public DrawInterface Draw {get; set;}
	public PrpObject Simulate {get; set;}
	public CoordinateInterface Transform {get; set;}
	public PrpObject Audio {get; set;}
	public PrpObject[] Interfaces {get; set;} = [];
	public PrpObject[] Modifiers {get; set;} = [];
	public PrpObject SceneNode {get; set;}
	public GameObject GameObject {get; set;}

	protected override void LoadObject(DatReader r) {
		r.SkipSynchedObject();
		Draw = (DrawInterface)ResolveReference(r);
		Simulate = ResolveReference(r);
		Transform = (CoordinateInterface)ResolveReference(r);
		Audio = ResolveReference(r);
		Interfaces = new PrpObject[r.ReadInt32()];
		for (var i = 0; i < Interfaces.Length; i++)
			Interfaces[i] = ResolveReference(r);
		Modifiers = new PrpObject[r.ReadInt32()];
		for (var i = 0; i < Modifiers.Length; i++)
			Modifiers[i] = ResolveReference(r);
		SceneNode = ResolveReference(r);
	}

	public void Spawn(GameObject parent) {
		GameObject = new GameObject(Name);
		GameObject.SetParent(parent, false);
	}
}
