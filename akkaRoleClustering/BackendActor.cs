//-----------------------------------------------------------------------
// <copyright file="TransformationBackend.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster;
using System;

namespace Samples.Cluster.Transformation
{
    public class BackendActor : UntypedActor
    {
        protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);
        private string port;

        public BackendActor(string port)
        {
            this.port = port;
            Console.WriteLine("BackendActor - constructor - port: {0}", port);
        }

        protected override void PreStart()
        {
            Console.WriteLine("BackendActor - PreStart - port: {0}", port);
            Cluster.Subscribe(Self, new[] { typeof(ClusterEvent.MemberUp) });
        }

        protected override void PostStop()
        {
            Console.WriteLine("BackendActor - PostStop - port: {0}", port);
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            if (message is Messages.Request)
            {
                var job = (Messages.Request) message;
                Sender.Tell(new Messages.Response(
                    job.ToString().ToUpper() + " response from BackendActor on port: " + port), Self);
            }
            else if (message is ClusterEvent.CurrentClusterState)
            {
                var state = (ClusterEvent.CurrentClusterState) message;
                foreach (var member in state.Members)
                {
                    if (member.Status == MemberStatus.Up)
                    {
                        Register(member);
                    }
                }
            }
            else if (message is ClusterEvent.MemberUp)
            {
                var memUp = (ClusterEvent.MemberUp) message;
                Register(memUp.Member);
            }
            else
            {
                Unhandled(message);
            }
        }

        protected void Register(Member member)
        {
            if(member.HasRole("frontend"))
                Context.ActorSelection(member.Address + "/user/frontend").Tell(Messages.BACKEND_REGISTRATION, Self);
        }
    }
}

