using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BobDust.Core.Threading;
using System.IO;
using SystemSocket = System.Net.Sockets.Socket;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
   class SocketPipeline : Pipeline
   {
      private SystemSocket _sendSocket;
      private SystemSocket _receiveSocket;
      private AutoResetEvent _waitHandle;

      public SocketPipeline(SystemSocket socket)
         : this(socket, socket)
      {
      }

      public SocketPipeline(SystemSocket sendSocket, SystemSocket receiveSocket)
         : base()
      {
         _sendSocket = sendSocket;
         _receiveSocket = receiveSocket;
         _waitHandle = new AutoResetEvent(false);
      }

      public override void Write(byte[] buffer)
      {
         _sendSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, (asyncResult) =>
         {
            SocketError errorCode;
            _sendSocket.EndSend(asyncResult, out errorCode);
            if (errorCode != SocketError.Success)
            {
            }
         }, null);
         //_sendSocket.Send(buffer);
      }

      public override int Read(byte[] buffer)
      {
         var bytesRead = 0;
         _receiveSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, (asyncResult) =>
         {
            SocketError errorCode;
            bytesRead = _receiveSocket.EndReceive(asyncResult, out errorCode);
            if (errorCode != SocketError.Success)
            {
            }
            _waitHandle.Set();
         }, null);
         _waitHandle.WaitOne();
         return bytesRead;
         //return _receiveSocket.Receive(buffer);
      }

      public override void Close()
      {
         base.Close();
         _waitHandle.Set();
      }

      protected override IBinarySequence Deserialize(byte[] bytes)
      {
         return BinarySequence.FromBytes<Package>(bytes);
      }

      protected override void Handle(Exception ex, IExceptionHandler source)
      {
         if (ex is SocketException)
         {
            var runnable = source as Runnable;
            if (runnable != null)
            {
               runnable.Stop();
            }
            _waitHandle.Set();
         }
         base.Handle(ex, source);
      }

   }
}
