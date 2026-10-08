namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface IBinarySequence
	{
		void Write(Stream stream);
		void Read(Stream stream);
		byte[] ToBytes();
	}
}
