using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	abstract class BinaryCommandBase : BinarySequence
	{
		public string OperationName { get; private set; }

		public BinaryCommandBase() : base() { }

		public BinaryCommandBase(string operationName)
		{
			OperationName = operationName;
		}

		public override void Read(BinaryReader reader)
		{
			const int bufferSize = 4096;
			using (var stream = new MemoryStream())
			{
				var buffer = new byte[bufferSize];
				var count = 0;
				while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
				{
					stream.Write(buffer, 0, count);
				}
				var formatter = new BinaryFormatter();
				stream.Seek(0, SeekOrigin.Begin);
				var deserialized = (BinaryCommandBase)formatter.Deserialize(stream);
				CopyFrom(deserialized);
			}
		}

		protected virtual void CopyFrom(BinaryCommandBase deserialized)
		{
			OperationName = deserialized.OperationName;
		}

		public override void Write(BinaryWriter writer)
		{
			using (var stream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, this);
				stream.Position = 0;
				writer.Write(stream.ToArray());
			}
		}
	}
}
