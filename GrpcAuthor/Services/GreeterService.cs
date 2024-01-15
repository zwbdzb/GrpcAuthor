
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using PingPongB;

namespace GrpcAuthor
{
    public class GreeterService : PingPong.PingPongBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override async Task PingPongHello(IAsyncStreamReader<Serve> requestStream,IServerStreamWriter<Catch> responseStream, ServerCallContext context)
        {
            try
            {
                if ("baiyun" != context.RequestHeaders.Get("node").Value)    // 接收请求头 header
                {
                  context.Status = new Status(StatusCode.PermissionDenied,"黑土只和白云打乒乓球");  // 设置响应状态码
                  await  Task.CompletedTask;
                  return;  
                }
                await context.WriteResponseHeadersAsync(new Metadata{   // 发送响应头header
                    { "node", "heitu" }
                });
                long  round = 0L;
                
                context.CancellationToken.Register(() => { 
                    Console.WriteLine($"乒乓球回合制结束, {context.Peer} : {round}");
                    context.ResponseTrailers.Add("round", round.ToString());  // 统计一个回合里双方有多少次对攻
                    context.Status = new Status(StatusCode.OK,"");  // 设置响应状态码
                });
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var asyncRequests = requestStream.ReadAllAsync(context.CancellationToken);
                    await foreach (var req in asyncRequests)
                    {
                        var send = RandomDirect();    // ToDo 想要实现一个 随时间衰减的概率算法，模拟对攻最后终止。
                        await responseStream.WriteAsync(new Catch
                        {
                            Direct = send,
                            Id = req.Id
                        });
                        Console.WriteLine($" {context.Peer} : 第{req.Id}次服务端收到 {req.Direct}, 第{req.Id + 1}次发送 {send}");
                        round++;
                    }
                }
                 
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            finally
            {
                Console.WriteLine($"乒乓球回合制结束");
            }
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
