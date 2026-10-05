using System.Reflection;
using BobDust.Core.Extensions.Reflection.Emit;
using System.Collections.Concurrent;
using BobDust.Rpc.Sockets.Serialization;
using BobDust.Rpc.Sockets.Abstractions;
using System.Reflection.Emit;

namespace BobDust.Rpc.Sockets.Builders
{
   public class ClientFactory
   {
      private static readonly ClientFactory _instance = new ClientFactory();
      public static ClientFactory Default { get { return _instance; } }

      private ConcurrentDictionary<(string Host, int Port), IChannel> _channels;

      private ClientFactory()
      {
         _channels = new ConcurrentDictionary<(string Host, int Port), IChannel>();
      }

      public T Get<T>(string host, int port)
      {
         return ConnectChannel(host, port).Mount<T>().As<T>();
      }

      public static async Task<IChannel> ConnectAsync(string host, int port)
      {
         return await _instance.ConnectChannelAsync(host, port);
      }

      private IChannel BuildChannel(string host, int port)
      {
         var key = (host, port);
         IChannel? channel = default;
         lock (_channels)
         {
            if (_channels.ContainsKey(key))
            {
               if (_channels[key] is Channel existing && existing.IsDisposed)
               {
                  _channels.TryRemove(key, out var _);
               }
               else
               {
                  channel = _channels[key];
               }
            }
         }
         if (channel != null)
         {
            return channel;
         }
         channel = new Channel(host, port, BuildCommand, BuildCommandResult, BuildClient);
         _channels[key] = channel;
         return channel;
      }

      private async Task<IChannel> ConnectChannelAsync(string host, int port)
      {
         var channel = BuildChannel(host, port);
         return await channel.ConnectAsync();
      }

      private IChannel ConnectChannel(string host, int port)
      {
         return BuildChannel(host, port).Connect();
      }

      public object BuildClient(Type contractType, Func<ICommand, ICommandResult> send, Func<ICommand, Task<ICommandResult>> sendAsync)
      {
         var baseType = typeof(Client);
         var invoke = baseType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
         var invokeAsync = baseType.GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic);
         var type = baseType.Implement(contractType, (method) => method.IsAsync() ? invokeAsync! : invoke!, EmitSyncBody, EmitAsyncBody);
         var constructor = type.GetConstructor([typeof(Func<string, string, IEnumerable<(string Name, object Value)>, ICommand>), typeof(Func<byte[], ICommandResult>), typeof(Func<ICommand, ICommandResult>), typeof(Func<ICommand, Task<ICommandResult>>)]);
         if (constructor == null)
         {
            throw new InvalidOperationException($"Constructor not found for type {type.FullName}");
         }
         try
         {
            Func<string, string, IEnumerable<(string Name, object Value)>, ICommand> commandFactory = BuildCommand;
            Func<byte[], ICommandResult> commandResultFactory = BuildCommandResult;
            var instance = constructor.Invoke([commandFactory, commandResultFactory, send, sendAsync]);
            return instance;
         }
         catch (Exception ex)
         {
            throw ex is { InnerException: { } inner } ? inner : ex;
         }
      }

      private static void EmitAsyncBody(ILGenerator emitter, Type contractType, MethodInfo method, MethodInfo invoke)
      {
         var returnType = method.ReturnType;
         var parameters = method.GetParameters();
         emitter.Emit(OpCodes.Ldarg_0);
         emitter.Emit(OpCodes.Ldc_I4, parameters.Length);
         emitter.Emit(OpCodes.Newarr, typeof(object));
         for (var i = 0; i < parameters.Length; i++)
         {
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Ldc_I4, i);
            emitter.Emit(OpCodes.Ldarg, i + 1);
            emitter.Emit(OpCodes.Stelem_Ref);
         }
         emitter.Emit(OpCodes.Ldstr, contractType.FullName!);
         emitter.Emit(OpCodes.Ldstr, method.Name);
         emitter.Emit(OpCodes.Ldc_I4, parameters.Length);
         emitter.Emit(OpCodes.Newarr, typeof(string));
         for (var i = 0; i < parameters.Length; i++)
         {
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Ldc_I4, i);
            emitter.Emit(OpCodes.Ldstr, parameters[i].Name!);
            emitter.Emit(OpCodes.Stelem_Ref);
         }
         if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
         {
            var taskResultType = returnType.GetGenericArguments()[0];
            emitter.Emit(OpCodes.Call, invoke.MakeGenericMethod(taskResultType));
         }
         else
         {
            emitter.Emit(OpCodes.Call, invoke.MakeGenericMethod(typeof(object)));
         }
         emitter.Emit(OpCodes.Ret);
      }

      private static void EmitSyncBody(ILGenerator emitter, Type contractType, MethodInfo method, MethodInfo invoke)
      {
         var parameters = method.GetParameters();
         emitter.Emit(OpCodes.Ldarg_0);
         emitter.Emit(OpCodes.Ldc_I4, parameters.Length);
         emitter.Emit(OpCodes.Newarr, typeof(object));
         for (var i = 0; i < parameters.Length; i++)
         {
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Ldc_I4, i);
            emitter.Emit(OpCodes.Ldarg, i + 1);
            emitter.Emit(OpCodes.Stelem_Ref);
         }
         emitter.Emit(OpCodes.Ldstr, contractType.FullName!);
         emitter.Emit(OpCodes.Ldstr, method.Name);
         emitter.Emit(OpCodes.Ldc_I4, parameters.Length);
         emitter.Emit(OpCodes.Newarr, typeof(string));
         for (var i = 0; i < parameters.Length; i++)
         {
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Ldc_I4, i);
            emitter.Emit(OpCodes.Ldstr, parameters[i].Name!);
            emitter.Emit(OpCodes.Stelem_Ref);
         }
         emitter.Emit(OpCodes.Call, invoke);
         if (method.ReturnType != typeof(void))
         {
            emitter.Emit(OpCodes.Castclass, method.ReturnType);
         }
         else
         {
            emitter.Emit(OpCodes.Pop);
         }
         emitter.Emit(OpCodes.Ret);
      }

      private ICommandResult BuildCommandResult(byte[] bytes)
      {
         return BinarySequence.FromBytes<BinaryCommandResult>(bytes);
      }

      private ICommand BuildCommand(string contractType, string method, IEnumerable<(string Name, object Value)> parameters)
      {
         return new BinaryCommand(contractType, method, parameters);
      }
   }

}
