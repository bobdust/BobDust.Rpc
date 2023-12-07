using System;
using System.Net.Sockets;
using System.Net;
using BobDust.Core.ExceptionHandling;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BobDust.Rpc.Sockets
{
    public abstract class Client : ExceptionHandler, IDisposable
   {
      private TcpClient _client;
      private CommandPipeline _pipeline;
      public bool IsDisposed { get; private set; }

      protected Client(string host, int port)
      {
         _client = new TcpClient();
         var serverEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
         _client.Connect(serverEndpoint);
         var sendSocket = _client.Client;
         var receiveSocket = new System.Net.Sockets.Socket(sendSocket.AddressFamily, sendSocket.SocketType, sendSocket.ProtocolType);
         receiveSocket.Connect(sendSocket.RemoteEndPoint);
         _pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize);
         _pipeline.OnException = (ex, source) =>
         {
            Dispose();
         };
         _pipeline.Open();
      }

      protected ICommandResult Send(ICommand command)
      {
         var result = _pipeline.Post(command);
         return (ICommandResult)result;
      }

      protected object Invoke(params object[] values)
      {
         var stackTrace = new StackTrace();
         var methodInfo = stackTrace.GetFrame(1).GetMethod();
         const string pattern = @"((?<interface>\w+).)?(?<methodName>\w+)";
         var methodName = Regex.Match(methodInfo.Name, pattern).Groups["methodName"].Value;
         var command = new XmlCommand(methodName);
         var parameters = methodInfo.GetParameters();
         for (var i = 0; i < parameters.Length; i++)
         {
            var parameter = parameters[i];
            command.Parameters[parameter.Name] = values[i];
         }
         var result = Send(command);
         if (result.ReturnValue != null)
         {
            return result.ReturnValue;
         }
         else if (result.Exception != null)
         {
            Handle(result.Exception, this);
         }
         return null;
      }

      protected void Subscribe(Action action)
      {
      }

      protected ICommandResult Deserialize(byte[] bytes)
      {
         return BinarySequence.FromBytes<XmlCommandResult>(bytes);
      }

      public void Dispose()
      {
         _pipeline.Dispose();
         _client.Close();
         IsDisposed = true;
      }
   }
}
