using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PingPongA;
using Google.Protobuf.WellKnownTypes;

namespace GrpcAuthorClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            //  打pingpong球
            var serverAddress = "http://localhost:5000";
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),    // tcp心跳探活
                EnableMultipleHttp2Connections = true               // 启用并发tcp连接
            };
            using var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions { 
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = 1024 * 1024 * 10,
                MaxSendMessageSize = 1024 * 1024 * 10,  
                HttpHandler = handler 
            });
            var client = new PingPong.PingPongClient(channel);
            AsyncDuplexStreamingCall<Serve,Catch>   duplexCall = null;
             Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}, 白云先发球");
            using (var cancellationTokenSource = new CancellationTokenSource(30*1000))
            {
                try
                {
                    duplexCall = client.PingPongHello(new Metadata
                    {
                        { "node", "baiyun" }
                    }, null, cancellationTokenSource.Token );
                    
                    var headers = await duplexCall.ResponseHeadersAsync;
                    if ("heitu" != headers.Get("node").Value)    // 接收响应头
                    {
                       throw new RpcException(new Status(StatusCode.PermissionDenied, "白云只和黑土打乒乓球"));
                    }
                    var direct = RandomDirect();
                    await duplexCall.RequestStream.WriteAsync(new Serve { Id= 1, Direct = direct }) ;
                    await foreach (var resp in duplexCall.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine($"第{resp.Id}次攻防，客户端发送{direct},客户端收到 {resp.Direct}");
                         direct = RandomDirect();
                        await duplexCall.RequestStream.WriteAsync(new Serve { Id= resp.Id+1 ,Direct =  direct });
                    }
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}打乒乓球结束");
                    if (duplexCall != null)
                    {
                        var tr = duplexCall.GetTrailers();   // 接受响应尾
                        var round  = tr.Get("round").Value.ToString();
                        Console.Write($" 进行了 {round} 次攻防）");
                    }
                }
                catch (RpcException ex)
                {
                    var trailers = ex.Trailers;
                    _ = trailers.GetValue("round");
                }
                catch(Exception ex) 
                {
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}打乒乓球（30s回合制）结束 未分出胜负，{ex.Message}");
                }
            }
            
            
            Console.ReadKey();
        }

        static Direction RandomDirect()
        {
            var ran = new Random();
            var ix = ran.Next(0, 4);
            var dir= new[] { "Front", "Back","Left", "Right",  }[ix];
            System.Enum.TryParse<Direction>(dir, out var direct);
            return direct;
        }
    }
     
}
