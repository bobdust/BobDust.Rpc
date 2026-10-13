namespace BobDust.Rpc.Sockets.Serialization;

[Serializable]
record JsonSerializable(string Type, byte[] Bytes);