using Dummy;
using Greet;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace client
{
    class Program
    {
        const string target = "127.0.0.1:50051";
        static async Task Main(string[] args)
        {
            Channel channel = new Channel(target, ChannelCredentials.Insecure);
            await channel.ConnectAsync().ContinueWith((task) =>
            {
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    Console.WriteLine("The client connected successfully");
            });

            //var client = new DummyService.DummyServiceClient(channel);

            var client = new GreetingService.GreetingServiceClient(channel);
            var greeting = new Greeting()
            {
                FirstName = "Mario",
                LastName = "Rossi"
            };

            // *** unary ***
            //var request = new GreetingRequest() { Greeting = greeting };
            //var response = client.Greet(request);

            // Console.WriteLine(response);

            // *** client streaming ***
            var request = new LongGreetingRequest() { Greeting = greeting };
            var stream = client.LongGreet();

            foreach (int i in Enumerable.Range(1, 10))
            {
                await stream.RequestStream.WriteAsync(request);
            }

            await stream.RequestStream.CompleteAsync();

            var response = await stream.ResponseAsync;

            Console.WriteLine(response);

            // *** server streaming ***
            //var request = new GreetingManyTimesRequest() { Greeting = greeting };
            //var response = client.GreetManyTimes(request);

            //while (await response.ResponseStream.MoveNext())
            //{
            //    Console.WriteLine(response.ResponseStream.Current.Result);
            //    await Task.Delay(100);
            //}



            channel.ShutdownAsync().Wait();
            Console.ReadKey();
        }
    }
}
