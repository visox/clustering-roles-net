//-----------------------------------------------------------------------
// <copyright file="TransformationMessages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Samples.Cluster.Transformation
{
    public sealed class Messages
    {
        public class Request
        {
            public Request(string text)
            {
                Text = text;
            }

            public string Text { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }

        public class Response
        {
            public Response(string text)
            {
                Text = text;
            }

            public string Text { get; private set; }

            public override string ToString()
            {
                return string.Format("Response ({0})", Text);
            }
        }

        public class RequestFailed
        {
            public RequestFailed(string reason, Request job)
            {
                Job = job;
                Reason = reason;
            }

            public string Reason { get; private set; }

            public Request Job { get; private set; }

            public override string ToString()
            {
                return string.Format("RequestFailed({0})", Reason);
            }
        }

        public const string BACKEND_REGISTRATION = "BackendRegistration";
    }
}

