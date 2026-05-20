using System.Numerics;

public class DatReader : IDisposable {
	private readonly BinaryReader _r;

	/// <summary>
	/// encrypted "dat" files use one of several headers, corresponding to different keys/ types of encryption <br/>
	/// we only need to support the specific encryption types used by the steam version of the game <br/>
	/// <br/>
	/// whatdoyousee - supported <br/>
	/// notthedroids - not supported <br/>
	/// briceissmart - not supported <br/>
	/// 0x88 0x42 0x87 0x0D - not supported <br/>
	/// </summary>
	public DatReader(Stream file) => _r = new(GetDecryptionStream(file));

	private static Stream GetDecryptionStream(Stream file) {
		var header = new byte[12];
		file.ReadExactly(header);
		if (Encoding.ASCII.GetBytes("whatdoyousee").SequenceEqual(header))
			throw new($"'whatdoyousee' type files not supported"); //XTEA [0x6c0a5452, 0x03827d0f, 0x3a170b92, 0x16db7fc2], 0x9E3779B9, 32
		if (Encoding.ASCII.GetBytes("notthedroids").SequenceEqual(header))
			throw new($"'notthedroids' type files not supported");
		if (Encoding.ASCII.GetBytes("bryceissmart").SequenceEqual(header))
			throw new($"'bryceissmart' type files not supported");
		if (new byte[] {0x88, 0x42, 0x87, 0x0D}.SequenceEqual(header.Take(4)))
			throw new($"'eoa' type files not supported");
		file.Position = 0;
		return file; //unencrypted
	}

	private static byte[] WhatDoYouSee(byte[] raw) { //XTEA
		var key = new uint[4] {0x6c0a5452, 0x03827d0f, 0x3a170b92, 0x16db7fc2};
		uint delta = 0x9E3779B9, rounds = 32;

		var decoded = new byte[BitConverter.ToInt32(raw, 12)];
		for (var i = 0; i < decoded.Length;) {
			var v0 = (uint)BitConverter.ToInt32(raw, i + 16);
			var v1 = (uint)BitConverter.ToInt32(raw, i + 20);
			uint sum = delta * rounds;
			for (uint j = 0; j < rounds; j++) {
				v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
				sum -= delta;
				v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
			}
			if (decoded.Length - i >= 8) {
				BitConverter.TryWriteBytes(new(decoded, i, 4), (int)v0); i += 4;
				BitConverter.TryWriteBytes(new(decoded, i, 4), (int)v1); i += 4;
			} else { //chunk smaller than 8 bytes, write last bytes manually
				var bytes = BitConverter.GetBytes((int)v0);
				for (var j = 0; j < 4 && i < decoded.Length; j++)
					decoded[i++] = bytes[j];
				bytes = BitConverter.GetBytes((int)v1);
				for (var j = 0; j < 4 && i < decoded.Length; j++)
					decoded[i++] = bytes[j];
			}
		}
		return decoded;
	}

	//expose all this stuff
	public int ReadInt32() => _r.ReadInt32();
	public short ReadInt16() => _r.ReadInt16();
	public byte ReadByte() => _r.ReadByte();
	public float ReadSingle() => _r.ReadSingle();
	public long Position {get => _r.BaseStream.Position; set => _r.BaseStream.Position = value;}

	public string ReadUruString() {
		var length = _r.ReadInt16();
		if ((length & 0xF000) == 0)
			_r.BaseStream.Position += 2;
		if ((length & 0xF000) != 0xF000)
			throw new("tried to read malformed string");
		length &= 0xFFF;
		if (length > 255)
			throw new("tried to read oversized string");
		var str = new byte[length];
		if (length > 0) {
			var b0 = _r.ReadByte();
			if ((b0 & 0x80) != 0) {
				str[0] = (byte)~b0;
				for (var i = 1; i < length; i++)
					str[i] = (byte)~_r.ReadByte();
			} else {
				str[0] = b0;
				for (var i = 1; i < length; i++)
					str[i] = _r.ReadByte();
			}
		}
		return Encoding.ASCII.GetString(str);
	}
	public string ReadWpString() => Encoding.ASCII.GetString(_r.ReadBytes(_r.ReadInt16()));

	public Transform ReadTransform() {
		var m = new float[4,4];
		for (int x = 0; x < 4; x++) {
			for (int y = 0; y < 4; y++)
				m[x,y] = _r.ReadSingle();
		}
		Matrix4x4.Decompose(new(
			m[0,0], m[1,0], m[2,0], m[3,0],
			m[0,1], m[1,1], m[2,1], m[3,1],
			m[0,2], m[1,2], m[2,2], m[3,2],
			m[0,3], m[1,3], m[2,3], m[3,3]
		), out var sca, out var rot, out var pos);
		return new(pos, rot, sca);
	}
	public Vector3 ReadVector3() => new(_r.ReadSingle(), _r.ReadSingle(), _r.ReadSingle());

	public void SkipSynchedObject() {
		var flags = _r.ReadInt32();
		if ((flags & 0x10) != 0) {
			var count = _r.ReadInt16();
			for (var i = 0; i < count; i++)
				ReadWpString();
		}
		if ((flags & 0x40) != 0) {
			var count = _r.ReadInt16();
			for (var i = 0; i < count; i++)
				ReadWpString();
		}
	}
	public void SkipHsBitVector() {
		var bvcount = _r.ReadInt32();
		for (var i = 0; i < bvcount; i++)
			Position += 4;
	}
	public void SkipBBox() {
		var flags = _r.ReadInt32();
		Position += 24; //mins
		if ((flags & 0x00000001) == 0) //off axis
			Position += 72; //corner, axis, x, y, axis, x, y, axis, x, y
		Position += 4; //mode, unused
	}

	void IDisposable.Dispose() {
		_r?.Dispose();
		GC.SuppressFinalize(this);
	}
}
