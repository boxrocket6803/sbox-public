using static Sandbox.VertexLayout;

namespace Prp;

[PrpType(TypeIndex.DrawableSpans)]
public class DrawableSpans : PrpObject {
	public class VertexFloatDecoder {
		private float Base {get; set;}
		private int Count {get; set;}
		public float Read(DatReader r, float granularity) {
			if (Count == 0) {
				Base = r.ReadSingle();
				Count = r.ReadInt16();
			}
			if (Count == 0)
				Log.Error("reached end of count prematurely");
			Count--;
			return Base + r.ReadInt16() / granularity;
		}
	}
	public class VertexColorDecoder {
		private float Base {get; set;}
		private int Count {get; set;}
		private bool RLE {get; set;}
		public float Read(DatReader r) {
			if (Count == 0) {
				Count = r.ReadInt16();
				RLE = (Count & 0x8000) != 0;
				if (RLE) {
					Base = r.ReadByte();
					Count &= 0x7FFF;
				}
			}
			if (Count == 0)
				Log.Error("reached end of count prematurely");
			Count--;
			return RLE ? Base : r.ReadByte();
		}
	}

	public struct MeshSpan {
		public int MaterialIndex {get; set;}
		public int Flags {get; set;}
		public Transform WorldTransform {get; set;}

		public int BufferIndex {get; set;}
		public int StartIndex {get; set;}
		public int IndexCount {get; set;}
	}
	public struct IndexBuffer {
		public int[] Indices {get; set;}
	}
	public struct BufferGroup {
		public struct Vertex {
			public Vector3 Position {get; set;}
			public Vector3 Normal {get; set;}
			public Color Color {get; set;}
			public Vector3[] TexCoords {get; set;}
			public float[] Weights {get; set;}
			public int[] Bones {get; set;} //absolute bone index
		}
		public struct SubMesh {
			public Vertex[] Vertices {get; set;}
		}
		public struct Surface {
			public Vector3[] Faces {get; set;} 
		}
		public struct CellGroup {
			public struct Cell {
				public int VertexStart {get; set;}
				public int ColorStart {get; set;}
				public int Length {get; set;}
			}
			public Cell[] Cells {get; set;}
		}
		public VertexFloatDecoder[] FloatDecoders {get; set;}
		public VertexColorDecoder[] ColorDecoders {get; set;}
		public SubMesh[] Meshes {get; set;}
		public Surface[] Surfaces {get; set;}
		public CellGroup[] Cells {get; set;}

		public BufferGroup(int weights, int uvs) {
			FloatDecoders = new VertexFloatDecoder[(6 + weights) + (3 * uvs)];
			Array.Fill(FloatDecoders, new());
			ColorDecoders = new VertexColorDecoder[4];
			Array.Fill(ColorDecoders, new());
		}
	}

	public PrpObject[] Materials {get; set;} = [];
	public PrpObject[] Fog {get; set;} = [];
	public Transform[] WorldToLocal {get; set;} = [];
	public Transform[] LocalToBone {get; set;} = [];
	public MeshSpan[] Meshes {get; set;} = [];
	public IndexBuffer[] Indices {get; set;} = [];
	public BufferGroup[] Buffers {get; set;} = [];

	protected override void LoadObject(DatReader r) {
		Log.Info($"load {Type} {Name}");
		r.Position += 12; //props, renderlevel, criteria
		Materials = new PrpObject[r.ReadInt32()];
		for (var i = 0; i < Materials.Length; i++)
			Materials[i] = ResolveReference(r); //TODO
		Meshes = new MeshSpan[r.ReadInt32()];
		for (var i = 0; i < Meshes.Length; i++) {
			var span = new MeshSpan();
			//span
			r.Position += 4; //subtype
			span.MaterialIndex = r.ReadInt32();
			r.Position += 64; //local to world
			span.WorldTransform = r.ReadTransform();
			span.Flags = r.ReadInt32();
			r.SkipBBox(); //local bounds
			r.SkipBBox(); //world bounds
			r.Position += 8; //matrix count, base matrix
			r.Position += 2; //local uvw chains
			r.Position += 4; //max bone index, pen bone index
			r.Position += 8; //min dist, max dist
			if ((span.Flags & 0x10) != 0)
				r.Position += 4; //water height
			//vertex span
			r.Position += 4; //group index
			r.Position += 8; //unused?
			r.Position += 4; //cell offset
			r.Position += 4; //vertex start index
			r.Position += 4; //vertex length
			//icicle
			r.Position += 4; //buffer index
			r.Position += 4; //start index
			r.Position += 4; //index count
			if ((span.Flags & 0x4) != 0)
				throw new("unexpected flag"); //more data to read here if this is ever hit
			Meshes[i] = span;
		}
		r.Position += 8; //unused, span count
		for (var i = 0; i < Meshes.Length; i++)
			r.Position += 4; //span index, spans are in the right order already though
		Fog = new PrpObject[Meshes.Length];
		for (var i = 0; i < Fog.Length; i++)
			Fog[i] = ResolveReference(r); //TODO these are probably all the same?
		if (Meshes.Length > 0) {
			r.SkipBBox(); //local bounds
			r.SkipBBox(); //world bounds
			r.SkipBBox(); //max world bounds (?)
		}
		//lights
		for (var i = 0; i < Meshes.Length; i++) {
			var span = Meshes[i];
			if ((span.Flags & 0x80) != 0) {
				var count = r.ReadInt32();
				for (var j = 0; j < count; j++)
					ResolveReference(r);
			}
			if ((span.Flags & 0x100) != 0) {
				var count = r.ReadInt32();
				for (var j = 0; j < count; j++)
					ResolveReference(r);
			}
		}
		r.Position += 4; //geometry span count (unused)
		//matricies
		var matrixcount = r.ReadInt32();
		WorldToLocal = new Transform[matrixcount];
		LocalToBone = new Transform[matrixcount];
		for (var i = 0; i < matrixcount; i++) {
			r.Position += 64; //local to world
			WorldToLocal[i] = r.ReadTransform();
			LocalToBone[i] = r.ReadTransform();
			r.Position += 64; //bone to local
		}
		Indices = new IndexBuffer[r.ReadInt32()];
		for (var i = 0; i < Indices.Length; i++) {
			var span = new IndexBuffer();
			r.Position += 4; //flags
			span.Indices = new int[r.ReadInt32()];
			for (var j = 0; j < span.Indices.Length; j++)
				span.Indices[j] = r.ReadInt32();
		}
		Buffers = new BufferGroup[r.ReadInt32()];
		for (var i = 0; i < Buffers.Length; i++) {
			var format = r.ReadByte();
			var uvwCount = (format & 0xF);
			var skinWeights = (format & 0x30) >> 4;
			var skinIndices = (format & 0x40) > 0;
			var span = new BufferGroup(skinWeights, uvwCount);
			r.Position += 4; //unused
			span.Meshes = new BufferGroup.SubMesh[r.ReadInt32()];
			for (var j = 0; j < span.Meshes.Length; j++) {
				Log.Info("start mesh");
				var mesh = new BufferGroup.SubMesh();
				mesh.Vertices = new BufferGroup.Vertex[r.ReadInt16()];
				for (var k = 0; k < mesh.Vertices.Length; k++) {
					var vertex = new BufferGroup.Vertex();
					var fldec = 0;
					//position
					vertex.Position = new(
						span.FloatDecoders[fldec++].Read(r, 1024),
						span.FloatDecoders[fldec++].Read(r, 1024),
						span.FloatDecoders[fldec++].Read(r, 1024)
					);
					Log.Info(vertex.Position);
					//TODO skinning
					if (skinWeights > 0)
						throw new("need to implement skinned meshes");
					/*
					vertex.Weights = new float[skinWeights];
					for (var l = 0; l < vertex.Weights.Length; l++)
						vertex.Weights[l] = span.FloatDecoders[fldec++].Read(r, 32768);
					if (skinWeights > 0 && skinIndices) {
						vertex.Bones = new int[4];
						for (var l = 0; l < 4; l++)
							vertex.Bones[l] = r.ReadInt32();
					}
					*/
					//normal
					vertex.Normal = new(
						r.ReadInt16() / 32767f,
						r.ReadInt16() / 32767f,
						r.ReadInt16() / 32767f
					);
					vertex.Normal = vertex.Normal.Normal;
					//color
					vertex.Color = new(
						span.ColorDecoders[0].Read(r),
						span.ColorDecoders[1].Read(r),
						span.ColorDecoders[2].Read(r),
						span.ColorDecoders[3].Read(r)
					);
					//uv
					vertex.TexCoords = new Vector3[uvwCount];
					for (var l = 0; l < vertex.TexCoords.Length; l++) {
						vertex.TexCoords[l] = new(
							span.FloatDecoders[fldec++].Read(r, 65536),
							span.FloatDecoders[fldec++].Read(r, 65536),
							span.FloatDecoders[fldec++].Read(r, 65536)
						);
					}
					mesh.Vertices[k] = vertex;
				}
				span.FloatDecoders = null;
				span.ColorDecoders = null;
				span.Meshes[j] = mesh;
			}
			//surfaces
			span.Surfaces = new BufferGroup.Surface[r.ReadInt32()];
			Log.Info($"surfaces {span.Surfaces.Length}");
			for (var j = 0; j < span.Surfaces.Length; j++) {
				var surface = new BufferGroup.Surface();
				surface.Faces = new Vector3[r.ReadInt32() / 3];
				for (var k = 0; k < surface.Faces.Length; k++)
					surface.Faces[k] = r.ReadInt16();
				span.Surfaces[j] = surface;
			}
			//cells
			span.Cells = new BufferGroup.CellGroup[span.Meshes.Length];
			for (var j = 0; j < span.Cells.Length; j++) {
				var cellgroup = new BufferGroup.CellGroup();
				cellgroup.Cells = new BufferGroup.CellGroup.Cell[r.ReadInt32()];
				for (var k = 0; k < cellgroup.Cells.Length; k++) {
					cellgroup.Cells[k] = new() {
						VertexStart = r.ReadInt32(),
						ColorStart = r.ReadInt32(),
						Length = r.ReadInt32()
					};
				}
				span.Cells[j] = cellgroup;
			}
			Buffers[i] = span;
		}
	}
}
