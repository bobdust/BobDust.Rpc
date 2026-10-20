using System.Threading.Tasks;

namespace BobDust.Rpc.Sockets.HelloWorld.Net4_8
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await BobDust.Rpc.Sockets.Tests.ClientSide.Program.Main(args);
        }
    }
}
