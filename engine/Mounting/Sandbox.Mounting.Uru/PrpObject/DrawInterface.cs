namespace Prp;

[PrpType(TypeIndex.DrawInterface)]
public class DrawInterface : PrpObject {
	public SceneObject Parent {get; set;}
	public DrawableSpans[] Drawable {get; set;} = [];

	protected override void LoadObject(DatReader r) {
		r.SkipSynchedObject();
		Parent = ResolveReference(r) as SceneObject;
		r.SkipHsBitVector();

		Drawable = new DrawableSpans[r.ReadInt32()];
		for (var i = 0; i < Drawable.Length; i++) {
			r.Position += 4; //subset group index (unused?)
			Drawable[i] = (DrawableSpans)ResolveReference(r);
		}
		var visregions = r.ReadInt32();
		for (var i = 0; i < visregions; i++)
			ResolveReference(r); //needn't worry about vis stuff
	}

	public override void Spawn() {
		foreach (var span in Drawable)
			Parent.GameObject.Components.Create<ModelRenderer>().Model = span.GetVmdl();
	}
}
