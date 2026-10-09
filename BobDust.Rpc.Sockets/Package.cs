using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	class Package : BinarySequence, IBinarySequence
	{
		private const int GuidSize = 16;

		public static implicit operator Package(byte[] bytes)
		{
			return FromBytes<Package>(bytes);
		}

		private static Package? _empty;
		public static Package Empty
		{
			get
			{
				return _empty ?? (_empty = new Package(Guid.Empty, 0, 0, Enumerable.Empty<byte>().ToArray()));
			}
		}

		public static int HeaderSize
		{
			get
			{
				return GuidSize + sizeof(int) + sizeof(int);
			}
		}

		public static Package Join(IEnumerable<Package> packages)
		{
			var orderedPackages = packages.OrderBy(p => p.Index);
			var package = Empty;
			foreach (var p in orderedPackages)
			{
				package = package.Concat(p);
			}
			return package;
		}

		public static IEnumerable<Package> Split(byte[] bytes, Guid token, int bufferSize)
		{
			var dataSize = bufferSize - HeaderSize;
			var length = bytes.Length;
			var count = length / dataSize + (length % dataSize > 0 ? 1 : 0);
			var position = 0;
			for (var index = 1; index <= count; index++)
			{
				var remainLength = length - position;
				var bytesCount = remainLength > dataSize ? dataSize : remainLength;
				byte[] packageData = bytes[position .. (position + bytesCount)];
				var package = new Package(token, index, count, packageData);
				position += bytesCount;
				yield return package;
			}
		}

		public static async IAsyncEnumerable<Package> SplitAsync(byte[] bytes, Guid token, int bufferSize)
		{
			var dataSize = bufferSize - HeaderSize;
			var length = bytes.Length;
			var count = length / dataSize + (length % dataSize > 0 ? 1 : 0);
			var position = 0;
			await foreach(var index in AsyncEnumerable.Range(1, count))
			{
				var remainLength = length - position;
				var bytesCount = remainLength > dataSize ? dataSize : remainLength;
				byte[] packageData = bytes[position .. (position + bytesCount)];
				var package = new Package(token, index, count, packageData);
				position += bytesCount;
				yield return package;
			}
		}

		public Guid Token { get; private set; }
		public int Index { get; private set; }
		public int Count { get; private set; }
		public byte[]? Data { get; private set; }

		public bool Deliverable
		{
			get
			{
				return Index == Count && Index == 1;
			}
		}

		public Package()
		{
		}

		public Package(Guid token, int index, int count, byte[] bytes)
		{
			Token = token;
			Index = index;
			Count = count;
			Data = bytes;
		}

		public Package Concat(Package package)
		{
			if (this == Empty)
			{
				return package;
			}
			return new Package(Token, Index, Count - 1, [.. Data ?? [], .. package.Data ?? []]);
		}

		private void Write(BinaryWriter writer)
		{
			writer.Write(Token.ToByteArray());
			writer.Write(Index);
			writer.Write(Count);
			writer.Write(Data!);
		}

		private void Read(BinaryReader reader)
		{
			Token = new Guid(reader.ReadBytes(GuidSize));
			Index = reader.ReadInt32();
			Count = reader.ReadInt32();
			Data = reader.ReadBytes((int)(reader.BaseStream.Length - HeaderSize));
		}

		public override void Write(Stream stream)
		{
			using var writer = new BinaryWriter(stream);
			Write(writer);
		}

		public override void Read(Stream stream)
		{
			using var reader = new BinaryReader(stream);
			Read(reader);
		}
	}
}
