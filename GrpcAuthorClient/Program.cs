using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcAuthorClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serverAddress = "https://localhost:5001";

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var client = new Greeter.GreeterClient(channel);

            using (var cancellationTokenSource = new CancellationTokenSource( 5* 1000))
            {

                try
                {
                    var duplexMessage = client.PingPongHello(null, null, cancellationTokenSource.Token);
                    await duplexMessage.RequestStream.WriteAsync(new HelloRequest { Id = 1, Name = "gridsum" }) ;

                    var asyncResp = duplexMessage.ResponseStream.ReadAllAsync();
                    await foreach (var resp in asyncResp)
                    {
                        var send = Reverse(resp.Message);
                        await duplexMessage.RequestStream.WriteAsync(new HelloRequest {Id= resp.Id, Name = send });
                        Console.WriteLine($"第{resp.Id}回合，客户端收到 {resp.Message}, 客户端发送{send}");
                    }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine("打乒乓球时间到了（客户端5s后终断gRpc连接）");
                }
            }

            Console.ReadKey();
        }

        static string Reverse(string str)
        {
            char[] arr = str.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
    }
     
}
