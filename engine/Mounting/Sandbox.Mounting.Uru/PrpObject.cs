using Azure;
using System.Data;
using System.Drawing;
using System.Reflection;

public class PrpObject {
	protected static readonly Sandbox.Diagnostics.Logger Log = new("PrpObject");

	[AttributeUsage(AttributeTargets.Class)]
	public class PrpTypeAttribute(TypeIndex type) : Attribute {
		public TypeIndex Type {get; set;} = type;
	}

	public enum TypeIndex {
		None						= 0x8000,
		SceneNode					= 0x0000,
		SceneObject					= 0x0001,
		MipMap						= 0x0004,
		CubicEnvironMap				= 0x0005,
		Layer						= 0x0006,
		GMaterial					= 0x0007,
		ParticleSystem				= 0x0008,
		BoundInterface				= 0x000C,
		AudioInterface				= 0x0011,
		WinAudio					= 0x0014,
		CoordinateInterface			= 0x0015,
		DrawInterface				= 0x0016,
		SpawnModifier				= 0x003D,
		HKPhysical					= 0x003F,
		LayerAnimation				= 0x0043,
		DrawableSpans				= 0x004C,
		DirectionalLightInfo		= 0x0055,
		OmniLightInfo				= 0x0056,
		PythonFileMod				= 0x00A2,
		SimulationInterface			= 0x001C,
		SoundBuffer					= 0x0029,
		PickingDetector				= 0x002B,
		LogicModifier				= 0x002D,
		ActivatorConditional		= 0x0032,
		ObjectInBoxConditional		= 0x0037,
		FacingConditional			= 0x003E,
		ViewFaceModifier			= 0x0040,
		AGModifier					= 0x006C,
		AGMasterMod					= 0x006D,
		CameraRegionDetector		= 0x006F,
		LineFollowMod				= 0x0071,
		OneShotMod					= 0x0077,
		RandomSoundMod				= 0x0079,
		ObjectInVolumeDetector		= 0x007B,
		ResponderModifier			= 0x007C,
		Win32StreamingSound			= 0x0084,
		Win32StaticSound			= 0x0096,
		CameraBrain					= 0x0099,
		CameraModifier				= 0x009B,
		CameraBrain_Avatar			= 0x009E,
		CameraBrain_Fixed			= 0x009F,
		ExcludeRegionModifier		= 0x00A4,
		VolumeSensorConditional		= 0x00A6,
		MsgForwarder				= 0x00A8,
		SittingModifier				= 0x00AE,
		RailCameraMod				= 0x00C0,
		MultiStageBehMod			= 0x00C1,
		CameraBrain_Circle			= 0x00C2,
		AnimEventModifier			= 0x00C4,
		ParticleCollisionDie		= 0x00C9,
		InterfaceInfoModifier		= 0x00CB,
		ParticleLocalWind			= 0x00D0,
		PointShadowMaster			= 0x00D5,
		ATCAnim						= 0x00F1,
		PanicLinkRegion				= 0x00FC,
		Stereizer					= 0x00FF,
		Occluder					= 0x0067,
		LimitedDirLightInfo			= 0x006A,
		SoftVolumeSimple			= 0x0088,
		SoftVolumeIntersect			= 0x008B,
		DynamicTextMap				= 0x00AD,
		EAXListenerMod				= 0x00E5,
		ImageLibMod					= 0x0122,
		PhysicalSndGroup			= 0x0127,
		ParticleCollisionBounce		= 0x00CA,
		VisRegion					= 0x0116,
		SpotLightInfo				= 0x0057,
		AvLadderMod					= 0x00B2,
		DynamicPuddleManager		= 0x00ED,
		WaveSet						= 0x00FB,
		DynamicEnvironmentMap		= 0x0106,
		ShadowCaster				= 0x00D4,
		DirectShadowMaster			= 0x00D6,
		DynamicFootManager			= 0x00E8,
		RelevanceRegion				= 0x011E,
		ClusterGroup				= 0x012B,
        SeekPointModifier			= 0x0076,
        AgeGlobalAnimation			= 0x00F2,
		ParticleFlockEffect			= 0x0123,
		FadeOpacityModifier			= 0x012F,
        SoftVolumeUnion				= 0x008A,
		SoftVolumeInvert			= 0x008C,
        LeafController				= 0x022B,
		ParticleEmitter				= 0x02d4,
		SimpleParticleGenerator		= 0x02d3,
		SpaceTree					= 0x0258,
		ScalarController			= 0x022F,
		SimplePositionController	= 0x0239,
		Matrix4x4Controller			= 0x0234,
		VolumeIsect					= 0x02F0,
		ConvexIsect					= 0x02F5,
		RefMessage					= 0x0203,
		GenRefMessage				= 0x0204,
		AnimationCommandMessage		= 0x0206,
		CameraMessage				= 0x020A,
		ActivatorMessage			= 0x0219,
		TimerCallbackMessage		= 0x024A,
		EnableMessage				= 0x024F,
		SoundMessage				= 0x0255,
		LinkToAgeMessage			= 0x02E1,
		NotifyMessage				= 0x02E8,
		ResponderEnableMessage		= 0x02FD,
		OneShotMessage				= 0x0302,
		ExcludeRegionMessage		= 0x0330,
		ArmatureEffectStateMessage	= 0x038E,
		SubWorldMessage				= 0x03BA,
		SimSuppressMessage			= 0x045B,
		OneTimeParticleGenerator	= 0x0331,
		EventCallbackMessage		= 0x024B,
		AnimationPath				= 0x02E6,
		SimpleRotationController	= 0x0237,
		CompoundRotationController	= 0x0238,
		SimpleScaleController		= 0x0236,
		CompoundPositionController	= 0x023A,
		TMController				= 0x023B,
	}

	public static Dictionary<int, PrpObject> Directory {get; set;} = [];

	public string File {get; set;}
	public int Hash {get; set;}
	public TypeIndex Type {get; set;}
	public string Name {get; set;}
	public int Offset {get; set;}
	public int Size {get; set;}

	public static PrpObject CreateFromDesc(string source, DatReader r) {
		var (flags, pageid, pagetype, type, name) = ReadHeader(r);
		var offset = r.ReadInt32();
		var size = r.ReadInt32();
		var inst = GetInstance(type);
		inst.Initialize(source, type, name, offset, size);
		inst.Hash = HashCode.Combine(flags, pageid, pagetype, type, name);
		Directory.Add(inst.Hash, inst);
		return inst;
	}

	public static PrpObject ResolveReference(DatReader r) {
		var exists = r.ReadByte();
		if (exists == 0)
			return null;
		var (flags, pageid, pagetype, type, name) = ReadHeader(r);
		var hash = HashCode.Combine(flags, pageid, pagetype, type, name);
		return Directory[hash];
	}

	private static (byte, int, short, TypeIndex, string) ReadHeader(DatReader r) {
		var flags = r.ReadByte();
		var pageid = r.ReadInt32();
		var pagetype = r.ReadInt16();
		if ((flags & 0x02) != 0)
			r.Position++; //loadmask
		var type = (TypeIndex)r.ReadInt16();
		var name = r.ReadUruString();
		if ((flags & 0x01) != 0)
			r.Position += 8; //cloneid, cloneplayerId
		return (flags, pageid, pagetype, type, name);
	}

	private static PrpObject GetInstance(TypeIndex type) {
		//use a type specific class if we've got one
		var classForType = typeof(PrpObject).Assembly.GetTypes().FirstOrDefault((t) => t.IsAssignableTo(typeof(PrpObject)) && t.GetCustomAttribute<PrpTypeAttribute>()?.Type == type);
		var inst = (PrpObject)classForType?.GetConstructor([]).Invoke([]) ?? new();
		return inst;
	}

	//this is kind of lame but saves me from having to make constructors for all the derived types
	private void Initialize(string source, TypeIndex type, string name, int offset, int size) {
		File = source;
		Type = type;
		Name = name;
		Offset = offset;
		Size = size;
	}

	private bool FinishedLoad;
	public void FinishLoad(DatReader r) {
		if (FinishedLoad)
			return;
		r.Position = Offset;
		//we know all this already, skip it
		r.Position += 2; //type
		ReadHeader(r); //header again
		//into object data now
		LoadObject(r);
		FinishedLoad = true;
	}
	protected virtual void LoadObject(DatReader r) { }
	public virtual void Spawn() { }
}
