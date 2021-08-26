using Greet;
using Grpc.Core;
using Mongo;
using Sqrt;
using System;
using System.Collections.Generic;
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
                var serverCrt = File.ReadAllText("ssl/server.crt");
                var serverKey = File.ReadAllText("ssl/server.key");
                var keyPair = new KeyCertificatePair(serverCrt, serverKey);
                var cacert = File.ReadAllText("ssl/ca.crt");

                var credentials = new SslServerCredentials(new List<KeyCertificatePair>() { keyPair }, cacert, true);

                server = new Server()
                {
                    Services = {
                        MongoService.BindService(new MongoServiceConcrete()),
                        GreetingService.BindService(new GreetingServiceConcrete()),
                        SqrtService.BindService(new SqrtServiceConcrete())
                    },
                    Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
                    //Ports = { new ServerPort("localhost", port, credentials) }
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
