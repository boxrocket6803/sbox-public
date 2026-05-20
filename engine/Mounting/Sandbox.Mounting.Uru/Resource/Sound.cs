class SoundLoader(string path) : ResourceLoader<UruMount> {
	string File {get; set;} = path;

	protected override object Load() => SoundFile.FromOgg(Path, System.IO.File.ReadAllBytes(File));
}
