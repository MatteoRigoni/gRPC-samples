using Greet;
using Grpc.Core;
using System;
using System.IO;
using static Greet.GreetingService;

namespace server
{
    class Program
    {
        const int port = 50051;
        static void Main(string[] args)
        {

            Server server = null;

            try
            {
                server = new Server()
                {
                    Services = { GreetingService.BindService(new GreetingServiceConcrete()) },
                    Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
                };

                server.Start();
                Console.WriteLine("Server listening on the port " + port);
                Console.ReadKey();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
            finally
            {
                if (server != null)
                    server.ShutdownAsync().Wait();
            }
        }
    }
}
