using System.IO;

namespace BobDust.Rpc.Sockets
{
	public interface IBinarySequence
   {
      void Write(BinaryWriter writer);
      void Read(BinaryReader reader);
      byte[] ToBytes();
   }
}
