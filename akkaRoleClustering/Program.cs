//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.Util.Internal;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Samples.Cluster.Transformation
{
    class Program
    {
        private static Config _clusterConfig;
        private static string _instructions = @"
Welcome to echo-actor cluster example in akka.net 
Type one of the following commands:
'help' - display instructions
'start-back <port>' - start a new backend node on given port
'start-front <port>' - start a new frontend node on given port
'stop-back <port>' - shutdown backend node on given port
'stop-front <port>' - shutdown frontend node on given port
'status <port>' - status of node on given port
'stop-all' - shutdowns all nodes
'quit' - terminates this program
            ";

        static void Main(string[] args)
        {


            var test = ConfigurationManager.GetSection("akka");
            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            _clusterConfig = section.AkkaConfig;

            Console.WriteLine(_instructions);

            var backendNodes = new Dictionary<string, ActorSystem>();
            var frontendNodes = new Dictionary<string, ActorSystem>();

            while (true)
            {
                var line = Console.ReadLine();
                var command = line.Split(' ')[0];
                var arguments = line.Split(' ').Drop(1);
                var quit = false;
                var port = "0";
                if(arguments.Count() >= 0)
                    port = arguments.Head();

                switch (command)
                {
                    case "help":
                        Console.WriteLine(_instructions);
                        break;
                    case "quit":
                        quit = true;
                        break;
                    case "start-back":
                        if (backendNodes.ContainsKey(port))
                        {
                            backendNodes[port].AwaitTermination();
                        }
                        backendNodes.AddOrSet(port, LaunchBackend(port));
                        break;
                    case "start-front":
                        if (frontendNodes.ContainsKey(port))
                        {
                            frontendNodes[port].AwaitTermination();
                        }
                        frontendNodes.AddOrSet(port, LaunchFrontend(port));
                        break;
                    case "stop-back":
                        if (backendNodes.ContainsKey(port))
                        {
                            backendNodes[port].Shutdown();
                            backendNodes.Remove(port);
                        }
                        break;
                    case "stop-front":
                        if (frontendNodes.ContainsKey(port))
                        {
                            frontendNodes[port].Shutdown();
                            frontendNodes.Remove(port);
                        }
                        break;
                    case "stop-all":
                        backendNodes.Values.ForEach(node => node.Shutdown());
                        frontendNodes.Values.ForEach(node => node.Shutdown());

                        backendNodes = new Dictionary<string, ActorSystem>();
                        frontendNodes = new Dictionary<string, ActorSystem>();
                        break;
                    case "status":
                        ActorSystem statusNode = null;
                        if (backendNodes.ContainsKey(port))
                        {
                            statusNode = backendNodes[port];
                            Console.WriteLine("node on port {0} is of backend type", port);
                        }
                        else if (frontendNodes.ContainsKey(port))
                        {
                            statusNode = frontendNodes[port];
                            Console.WriteLine("node on port {0} is of frontend type", port);
                        }
                        if (statusNode == null)
                        {
                            Console.WriteLine("No node with such a port");
                        }
                        else
                        {
                            Console.WriteLine("Node - ActorSystem: {0}", statusNode);
                            Console.WriteLine("Name: {0}", statusNode.Name);
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown instruction, type 'help' to display instructions");
                        break;
                }

                if (quit)
                    break;
            }

            backendNodes.Values.ForEach(node => node.Shutdown());
            frontendNodes.Values.ForEach(node => node.Shutdown());

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        static ActorSystem LaunchBackend(string port)
        {
            Console.WriteLine("Start - actorsystem for backend node - port: {0}", port);

            var config =
                    ConfigurationFactory.ParseString("akka.remote.helios.tcp.port=" + port)
                    .WithFallback(ConfigurationFactory.ParseString("akka.cluster.roles = [backend]"))
                        .WithFallback(_clusterConfig);

            var system = ActorSystem.Create("ClusterSystem", config);
            system.ActorOf(Props.Create<BackendActor>(port), "backend");

            Console.WriteLine("End - actorsystem for backend node - port: {0}", port);

            return system;
        }

        static ActorSystem LaunchFrontend(string port)
        {
            Console.WriteLine("Start - actorsystem for frontend node - port: {0}", port);

            var config =
                    ConfigurationFactory.ParseString("akka.remote.helios.tcp.port=" + port)
                    .WithFallback(ConfigurationFactory.ParseString("akka.cluster.roles = [frontend]"))
                        .WithFallback(_clusterConfig);

            var system = ActorSystem.Create("ClusterSystem", config);

            var frontend = system.ActorOf(Props.Create<FrontendActor>(port), "frontend");
            var interval = TimeSpan.FromSeconds(2);
            var timeout = TimeSpan.FromSeconds(5);
            var counter = new AtomicCounter();
            system.Scheduler.Advanced.ScheduleRepeatedly(interval, interval,
                () => frontend.Ask(new Messages.Request("request-" + counter.GetAndIncrement() + " from frontend node port " + port), timeout)
                    .ContinueWith( r =>
                    {
                        if (!r.IsCanceled)
                        {
                            Console.WriteLine(r.Result);
                            // Debug.WriteLine(r.Result);
                        }
                    }));

            Console.WriteLine("End - actorsystem for frontend node - port: {0}", port);

            return system;
        }
    }
}

