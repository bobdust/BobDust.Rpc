using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BobDust.Rpc.Sockets
{
	public class Package : BinarySequence, IBinarySequence
   {
      private const int GuidSize = 16;


      public static implicit operator Package(byte[] bytes)
      {
         return BinarySequence.FromBytes<Package>(bytes);
      }

      private static Package _empty;
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

      public Guid Token { get; private set; }
      public int Index { get; private set; }
      public int Count { get; private set; }
      public byte[] Data { get; private set; }

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
         return new Package(Token, Index, Count - 1, Data.Concat(package.Data).ToArray());
      }

      public override void Write(BinaryWriter writer)
      {
         writer.Write(Token.ToByteArray());
         writer.Write(Index);
         writer.Write(Count);
         writer.Write(Data);
      }

      public override void Read(BinaryReader reader)
      {
         Token = new Guid(reader.ReadBytes(GuidSize));
         Index = reader.ReadInt32();
         Count = reader.ReadInt32();
         Data = reader.ReadBytes((int)(reader.BaseStream.Length - HeaderSize));
      }
   }
}
