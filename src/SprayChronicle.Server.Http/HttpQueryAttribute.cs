using System;

namespace SprayChronicle.Server.Http
{
    [AttributeUsageAttribute(AttributeTargets.Class)]
    public class HttpQueryAttribute : HttpAttribute
    {
        public readonly string Template;

        public HttpQueryAttribute(string template)
        {
            Template = template;
            Method = "GET";
        }
    }
}
