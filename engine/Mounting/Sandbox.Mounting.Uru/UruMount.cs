using System.Reflection;
using static PrpObject;

public partial class UruMount : BaseGameMount {
	public override string Ident => "uru";
	public override string Title => "Uru: Complete Chronicles";
	const long AppId = 63650;

	private string GameDir {get; set;}

	protected override void Initialize(InitializeContext context) {
		if (!context.IsAppInstalled(AppId))
			return;
		GameDir = context.GetAppDirectory(AppId);
		IsInstalled = true;
	}

	protected override Task Mount(MountContext context) {
		foreach (var file in System.IO.Directory.GetFiles(GameDir, "*.*", SearchOption.AllDirectories)) {
			var ext = Path.GetExtension(file)?.ToLower();
			var rel = Path.GetRelativePath(GameDir, file);
			switch (ext) {
				//sounds are just ogg, the game converts to wav at runtime using an exe, we can play them direct
				case ".ogg":
					context.Add(ResourceType.Sound, rel, new SoundLoader(file));
					break;
				//the rest is their generic scene format, need to read the file to see whats in it
				case ".age":
				case ".prp":
				case ".sdl":
				case ".fni":
					try {
						PrpFile.Register(context, Ident, file, rel);
					} catch (Exception e) {Log.Error(e);}
					break;
			}
		}
		IsMounted = true;
		return Task.CompletedTask;
	}
}
