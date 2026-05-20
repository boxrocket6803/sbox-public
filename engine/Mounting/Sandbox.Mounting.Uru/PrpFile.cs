public static class PrpFile {
	public static void Classify(MountContext context, string path, string destination) {
		using var r = new DatReader(File.OpenRead(path));
		var objects = ReadObjects(path, r);
		if (objects.Any((o) => o.Type == PrpObject.TypeIndex.SceneObject))
			context.Add(ResourceType.PrefabFile, destination, new SceneLoader(path, objects));
	}

	private static List<PrpObject> ReadObjects(string source, DatReader r) {
		if (r.ReadInt16() != 5)
			throw new("bad/unknown prp header");
		List<PrpObject> objects = [];
		//header, don't care about most of this
		r.Position += 8;
		r.ReadUruString(); //agename
		r.ReadUruString(); //district
		r.ReadUruString(); //pagename
		r.Position += 20;
		r.Position = r.ReadInt32(); //jump to object index offset
		//object index
		var typecount = r.ReadInt32();
		for (var i = 0; i < typecount; i++) {
			r.Position += 2; //type (desc also has the type, trust that more)
			var count = r.ReadInt32();
			for (var j = 0; j < count; j++)
				objects.Add(PrpObject.CreateFromDesc(source, r));
		}
		return objects;
	}
}
