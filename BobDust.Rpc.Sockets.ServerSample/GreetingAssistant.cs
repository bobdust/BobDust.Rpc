using BobDust.Rpc.Sockets.HelloWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BobDust.Rpc.Sockets.ServerSample
{
	public class GreetingAssistant : IGreetingAssistant
	{
		public string Hello()
		{
			return "Hello, world!";
		}
	}
}
