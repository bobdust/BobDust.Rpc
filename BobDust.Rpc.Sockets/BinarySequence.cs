using BobDust.Rpc.Sockets.Abstractions;
using System;
using System.IO;

namespace BobDust.Rpc.Sockets
{
	[Serializable]
	abstract class BinarySequence
	{

		public static T FromBytes<T>(byte[] bytes)
		   where T : IBinarySequence, new()
		{
			var instance = new T();
			using (var stream = new MemoryStream(bytes))
			{
				using (var reader = new BinaryReader(stream))
				{
					instance.Read(reader);
				}
			}
			return instance;
		}

		public virtual byte[] ToBytes()
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(stream))
				{
					Write(writer);
				}
				return stream.ToArray();
			}
		}

		public abstract void Write(BinaryWriter writer);

		public abstract void Read(BinaryReader reader);
	}
}
