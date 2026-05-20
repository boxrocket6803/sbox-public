using System.Numerics;
namespace Prp;

[PrpType(TypeIndex.CoordinateInterface)]
public class CoordinateInterface : PrpObject {
	public SceneObject Parent {get; set;}
	public SceneObject[] Children {get; set;} = [];

	public Transform Local {get; set;}

	protected override void LoadObject(DatReader r) {
		r.SkipSynchedObject();
		Parent = ResolveReference(r) as SceneObject;
		r.SkipHsBitVector();

		r.Position += 64; //local to parent
		Local = r.ReadTransform(); //parent to local
		r.Position += 128; //local to world, world to local

		Children = new SceneObject[r.ReadInt32()];
		for (var i = 0; i < Children.Length; i++)
			Children[i] = ResolveReference(r) as SceneObject;
	}

	public override void Spawn() {
		Parent.GameObject.LocalTransform = Local;
		foreach (var child in Children)
			child.GameObject.SetParent(Parent.GameObject, false);
	}
}
