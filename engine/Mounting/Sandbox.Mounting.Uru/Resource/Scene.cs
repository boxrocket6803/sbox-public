class SceneLoader(string path, List<PrpObject> objects) : ResourceLoader<UruMount> {
	string File {get; set;} = path;
	List<PrpObject> Objects {get; set;} = objects;

	protected override object Load() {
		var scene = new PrefabBuilder().WithName(Path);
		using (scene.Scope()) {
			var root = new GameObject(Objects.FirstOrDefault((o) => o.Type == PrpObject.TypeIndex.SceneNode)?.Name ?? "SceneNode");
			foreach (var node in Objects) {
				node.FinishLoad();
				//scene objects need to get their gameobjects in before anything else so they can be reparented
				if (node is Prp.SceneObject so)
					so.Spawn(root);
			}
			foreach (var node in Objects)
				node.Spawn();
		}
		return scene.Create();
	}
}
