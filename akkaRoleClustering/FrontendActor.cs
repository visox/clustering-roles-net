//-----------------------------------------------------------------------
// <copyright file="TransformationFrontend.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Akka.Actor;
using Akka.Routing;
using System;

namespace Samples.Cluster.Transformation
{
    public class FrontendActor : UntypedActor
    {
        protected List<IActorRef> Backends = new List<IActorRef>();
        protected IActorRef router = null;
        protected int Jobs = 0;
        private string port;

        public FrontendActor(string port)
        {
            this.port = port;
            Console.WriteLine("FrontendActor - constructor - port: {0}", port);
        }

        protected override void PreStart()
        {
            Console.WriteLine("FrontendActor - PreStart - port: {0}", port);
        }

        protected override void PostStop()
        {
            Console.WriteLine("FrontendActor - PostStop - port: {0}", port);
        }

        protected override void OnReceive(object message)
        {
            if (message is Messages.Request && Backends.Count == 0)
            {
                var job = (Messages.Request) message;
                Sender.Tell(new Messages.RequestFailed("Service unavailable, try again later.", job), Sender);
            }
            else if (message is Messages.Request)
            {
                var job = (Messages.Request)message;
                Jobs++;
                router.Forward(job);
            }
            else if (message.Equals(Messages.BACKEND_REGISTRATION))
            {
                Context.Watch(Sender);
                Backends.Add(Sender);
                RefreshRouter();
            }
            else if (message is Terminated)
            {
                var terminated = (Terminated) message;
                Backends.Remove(terminated.ActorRef);
                RefreshRouter();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void RefreshRouter()
        {
            if(router != null)
            {
                router.Tell(PoisonPill.Instance);
            }
            router = Context.System.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup(Backends)));
        }
    }
}

