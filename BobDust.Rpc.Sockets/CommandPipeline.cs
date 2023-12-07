using System;
using System.Threading;
using System.Collections.Concurrent;

namespace BobDust.Rpc.Sockets
{
	public class CommandPipeline : PipelineDecorator
   {
      protected const int MillisecondsTimeout = 60 * 1000;

      private Func<byte[], IBinarySequence> _deserialize;

      public CommandPipeline(IPipeline pipeline, Func<byte[], IBinarySequence> deserializer)
         : base(pipeline)
      {
         _deserialize = deserializer;
      }

      protected override IBinarySequence Deserialize(byte[] bytes)
      {
         return _deserialize(bytes);
      }

      protected override Guid CreateDataToken()
      {
         return ChannelContext.CurrentThreadContext.Token;
      }

      protected override void DataReceived(Guid token)
      {
         var context = ChannelContext.Get(token);
         if (context == null)
         {
            using (context = ChannelContext.New(token))
            {
               base.DataReceived(token);
            }
         }
         else
         {
            context.WaitHandle.Set();
         }
      }

      public IBinarySequence Post(IBinarySequence request)
      {
         using (var context = ChannelContext.New())
         {
            Send(request);
            context.WaitHandle.WaitOne(MillisecondsTimeout);
            var response = Receive(context.Token);
            if (response == null)
            {
               throw new TimeoutException();
            }
            return response;
         }
      }

      public void Post(IBinarySequence request, Action<IBinarySequence> responseReceived)
      {
         Func<IBinarySequence> action = () =>
         {
            using (var context = ChannelContext.New())
            {
               Send(request);
               context.WaitHandle.WaitOne();
               var response = Receive(context.Token);
               return response;
            }
         };
         action.BeginInvoke((asynResult) => {
            var response = action.EndInvoke(asynResult);
            if (response != null)
            {
               responseReceived(response);
            }
         }, null);
      }


      class ChannelContext : IDisposable
      {
         private static ConcurrentDictionary<Guid, ChannelContext> _contexts;

         static ChannelContext()
         {
            _contexts = new ConcurrentDictionary<Guid, ChannelContext>();
         }

         private ChannelContext(Guid token)
         {
            Token = token;
            WaitHandle = new AutoResetEvent(false);
         }

         public Guid Token { get; private set; }
         public AutoResetEvent WaitHandle { get; private set; }

         public static ChannelContext CurrentThreadContext
         {
            get
            {
               var threadId = Thread.CurrentThread.ManagedThreadId;
               var context = AppDomain.CurrentDomain.GetData(threadId.ToString());
               return (ChannelContext)context;
            }
            private set
            {
               AppDomain.CurrentDomain.SetData(Thread.CurrentThread.ManagedThreadId.ToString(), value);
            }
         }

         public static ChannelContext Get(Guid token)
         {
            if (_contexts.ContainsKey(token))
            {
               return _contexts[token];
            }
            return null;
         }

         public static ChannelContext New()
         {
            return New(Guid.NewGuid());
         }

         public static ChannelContext New(Guid token)
         {
            var context = new ChannelContext(token);
            _contexts[token] = context;
            CurrentThreadContext = context;
            return context;
         }

         public void Dispose()
         {
            WaitHandle.Set();
            WaitHandle.Dispose();
            ChannelContext context;
            _contexts.TryRemove(Token, out context);
         }
      }
   }
}
