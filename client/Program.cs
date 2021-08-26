using Dummy;
using Greet;
using Grpc.Core;
using Mongo;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client
{
    class Program
    {
        const string target = "127.0.0.1:50051";
        static async Task Main(string[] args)
        {
            //await BasicDemo();
            await MongoDemo();
        }

        private static async Task MongoDemo()
        {
            Channel channel = new Channel(target, ChannelCredentials.Insecure);

            await channel.ConnectAsync().ContinueWith((task) =>
            {
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    Console.WriteLine("The client connected successfully");
                else
                    Console.WriteLine("ERROR: " + task.Status.ToString());
            });

            var client = new MongoService.MongoServiceClient(channel);

            //CreateBlog(client);
            //ReadBlog(client);
            //UpdateBlog(client);
            //DeleteBlog(client);

            channel.ShutdownAsync().Wait();
            Console.ReadKey();
        }

        private static void ReadBlog(MongoService.MongoServiceClient client)
        {
            try
            {
                var response = client.ReadBlog(new ReadBlogRequest()
                {
                    BlogId = "61275fd473733887951c63a8"
                });

                Console.WriteLine(response.Blog.ToString());
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex.Status.Detail);
            }
        }

        private static void UpdateBlog(MongoService.MongoServiceClient client)
        {
            try
            {
                var response = client.UpdateBlog(new UpdateBlogRequest()
                {
                    Blog = new Blog() {
                       Id= "61275fd473733887951c63a8",
                       AuthorId = "updated author",
                       Content = "updated content",
                       Title = "updated title"
                    }
                });

                Console.WriteLine(response.Blog.ToString());
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex.Status.Detail);
            }
        }

        private static void DeleteBlog(MongoService.MongoServiceClient client)
        {
            try
            {
                var response = client.DeleteBlog(new DeleteBlogRequest()
                {
                    BlogId = "61275fd473733887951c63a8"
                });

                Console.WriteLine(response.BlogId.ToString());
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex.Status.Detail);
            }
        }

        private static void CreateBlog(MongoService.MongoServiceClient client)
        {
            var response = client.CreateBlog(new CreateBlogRequest()
            {
                Blog = new Blog()
                {
                    AuthorId = "John",
                    Title = "John's blog",
                    Content = "Content of the blog"
                }
            });

            Console.WriteLine("The blog with id " + response.Blog.Id + " has been created");
        }

        private static async Task BasicDemo()
        {
            var clientCrt = File.ReadAllText("ssl/client.crt");
            var clientKey = File.ReadAllText("ssl/client.key");
            var caCrt = File.ReadAllText("ssl/ca.crt");

            var channelCredentials = new SslCredentials(caCrt, new KeyCertificatePair(clientCrt, clientKey));

            Channel channel = new Channel(target, ChannelCredentials.Insecure);
            //Channel channel = new Channel(target, channelCredentials);

            await channel.ConnectAsync().ContinueWith((task) =>
            {
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    Console.WriteLine("The client connected successfully");
                else
                    Console.WriteLine("ERROR: " + task.Status.ToString());
            });

            //var client = new DummyService.DummyServiceClient(channel);

            var clientGreeting = new GreetingService.GreetingServiceClient(channel);
            var clientSqrt = new Sqrt.SqrtService.SqrtServiceClient(channel);
            var greeting = new Greeting()
            {
                FirstName = "Mario",
                LastName = "Rossi"
            };

            DoUnary(clientGreeting, greeting);
            //DoUnarySqrt(clientSqrt, -9);
            //await DoClientStreaming(clientGreeting, greeting);
            //await DoServerStreaming(clientGreeting, greeting);
            //await DoBidirectionalStreaming(clientGreeting, greeting);

            channel.ShutdownAsync().Wait();
            Console.ReadKey();
        }

        private static void DoUnary(GreetingService.GreetingServiceClient client, Greeting greeting)
        {
            var request = new GreetingRequest() { Greeting = greeting };
            var response = client.Greet(request);

            Console.WriteLine(response);
        }

        private static void DoUnarySqrt(Sqrt.SqrtService.SqrtServiceClient client, int number)
        {
            try
            {
                var request = new Sqrt.SqrtRequest() { Number = number };
                var response = client.sqrt(request, deadline: DateTime.UtcNow.AddMilliseconds(100));

                Console.WriteLine(response);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                Console.WriteLine(ex.Status.Detail);
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task DoServerStreaming(GreetingService.GreetingServiceClient client, Greeting greeting)
        {
            var request = new GreetingManyTimesRequest() { Greeting = greeting };
            var response = client.GreetManyTimes(request);

            while (await response.ResponseStream.MoveNext())
            {
                Console.WriteLine(response.ResponseStream.Current.Result);
                await Task.Delay(100);
            }
        }

        private static async Task DoClientStreaming(GreetingService.GreetingServiceClient client, Greeting greeting)
        {
            var request = new LongGreetingRequest() { Greeting = greeting };
            var stream = client.LongGreet();

            foreach (int i in Enumerable.Range(1, 10))
            {
                await stream.RequestStream.WriteAsync(request);
            }

            await stream.RequestStream.CompleteAsync();

            var response = await stream.ResponseAsync;

            Console.WriteLine(response);
        }

        private static async Task DoBidirectionalStreaming(GreetingService.GreetingServiceClient client, Greeting greeting)
        {
            var stream = client.GreetEveryone();

            var responseStreamTask = Task.Run(async () =>
            {
                while (await stream.ResponseStream.MoveNext())
                {
                    Console.WriteLine("Received: " + stream.ResponseStream.Current.Result);
                    await Task.Delay(2000);
                }
            });

            Greeting[] greetings =
            {
                new Greeting() { FirstName = "Mario", LastName = "Rossi" },
                new Greeting() { FirstName = "Luca", LastName = "Bianchi" },
                new Greeting() { FirstName = "Giuseppe", LastName = "Verdi" }
            };

            foreach (var g in greetings)
            {
                await stream.RequestStream.WriteAsync(new GreetingEveryoneRequest()
                {
                    Greeting = g
                });
            }

            await stream.RequestStream.CompleteAsync();
            await responseStreamTask;
        }
    }
}
