using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	[Serializable]
	public abstract class BinarySequence
	{
		public static T FromBytes<T>(byte[] bytes)
		   where T : IBinarySequence, new()
		{
			var instance = new T();
			using var stream = new MemoryStream(bytes);
			instance.Read(stream);
			return instance;
		}

		public virtual byte[] ToBytes()
		{
			using var stream = new MemoryStream();
			Write(stream);
			return stream.ToArray();
		}

		public abstract void Write(Stream stream);

		public abstract void Read(Stream stream);
	}
}
