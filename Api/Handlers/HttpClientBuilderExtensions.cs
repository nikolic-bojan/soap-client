using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Api.Handlers
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddTraceLogHandler(this IHttpClientBuilder builder, Func<HttpResponseMessage, bool> shouldLog)
        {
            return builder.AddHttpMessageHandler((services) => new TraceLogHandler(services.GetRequiredService<IHttpContextAccessor>(), shouldLog));
        }

        public static IHttpClientBuilder AddTraceLogHandler(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler((services) => new TraceLogHandler(services.GetRequiredService<IHttpContextAccessor>(), (HttpResponseMessage) => { return false; }));
        }
    }
}