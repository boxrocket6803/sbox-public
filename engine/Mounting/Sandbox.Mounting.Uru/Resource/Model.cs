public class ModelLoader(Prp.DrawableSpans spans) : ResourceLoader<UruMount> {
	public Prp.DrawableSpans Spans {get; set;} = spans;

	protected override object Load() {
		Spans.FinishLoad();
		var m = Model.Builder.WithName(Path);
		foreach (var span in Spans.Indices) {
			foreach (var submesh in span.Indices) {
				var icicle = Spans.Meshes[submesh];
				var group = Spans.Buffers[icicle.GroupIndex];

				var vb = new VertexBuffer();
				vb.Init(true);
				var tv = new List<Vector3>(icicle.VertexCount);
				var ti = new List<int>(icicle.IndexCount);

				//vertex
				for (var i = icicle.VertexStart; i < icicle.VertexCount + icicle.VertexStart; i++) {
					var vertex = group.Vertices[i];
					vb.Add(new() {
						Position = vertex.Position,
						Normal = vertex.Normal,
						Tangent = new(vertex.Normal, -1),
						Color = vertex.Color,
						TexCoord0 = vertex.TexCoord(0),
						TexCoord1 = vertex.TexCoord(1), 
					});
					tv.Add(vertex.Position);
				}
				//index
				var surface = group.Surfaces[icicle.IndexBuffer];
				for (var i = icicle.IndexStart; i < icicle.IndexCount + icicle.IndexStart; i++) {
					var index = surface.Indices[i] - icicle.VertexStart;
					vb.AddRawIndex(index);
					ti.Add(index);
				}

				var mesh = new Mesh(Material.Load("materials/default.vmat"));
				mesh.CreateBuffers(vb);
				m.AddMesh(mesh);
			}
		}
		return m.Create();
	}
}
