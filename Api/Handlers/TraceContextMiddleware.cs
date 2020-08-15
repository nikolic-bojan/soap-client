using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Handlers
{
    public class TraceContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TraceContextMiddleware> _logger;
        private static readonly string[] IgnorePaths = { "/", "/favicon.ico" };

        public TraceContextMiddleware(RequestDelegate next, ILogger<TraceContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var state = new Dictionary<string, object>();

            // Put here all properties you would like to track on a per-request basis

            if (context != null)
            {
                if (context.Request.Query.ContainsKey(TraceLogHandler.TraceMe))
                {
                    state.TryAdd("TraceMe", context.Request.Query[TraceLogHandler.TraceMe]);
                }

                var endpoint = context.GetEndpoint();
                var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (controllerActionDescriptor != null)
                {
                    state.TryAdd("RequestController", controllerActionDescriptor.ControllerName);
                    state.TryAdd("RequestAction", controllerActionDescriptor.ActionName);
                }


                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    state.TryAdd("RequestUser", userId);
                }

                var request = context.Request;
                var headers = request.Headers;
                state.TryAdd("RequestMethod", request.Method);
                state.TryAdd("RequestHost", request.Host.Host);
                state.TryAdd("RequestQueryString", request.QueryString.ToString());
                if (headers.TryGetValue("User-Agent", out var useragent))
                {
                    state.TryAdd("RequestUserAgent", useragent);
                }                
                string clientIp;
                if ((clientIp = GetClientIp(context)) != null)
                {
                    state.TryAdd("RequestClientip", clientIp.ToString());
                }
            }

            using (_logger.BeginScope(state))
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context).ConfigureAwait(false);

                // We want to filter out paths like "/", "/swagger" etc.
                if (context != null && !IgnorePaths.Contains(context.Request.Path.Value))
                {
                    _logger.LogInformation("Request to {RequestPath} returned status code {StatusCode}", context.Request.Path.Value, context.Response.StatusCode);
                }
            }
        }

        internal static string GetClientIp(HttpContext context)
        {
            string result = null;

            if (context.Request.Headers != null)
            {
                //the X-Forwarded-For (XFF) HTTP header field is a de facto standard for identifying the originating IP address of a client
                //connecting to a web server through an HTTP proxy or load balancer

                var forwardedHeader = context.Request.Headers["X-Forwarded-For"];
                if (!StringValues.IsNullOrEmpty(forwardedHeader))
                {
                    result = forwardedHeader.FirstOrDefault();
                }
            }

            //if this header not exists try get connection remote IP address
            if (string.IsNullOrEmpty(result) && context.Connection.RemoteIpAddress != null)
            {
                result = context.Connection.RemoteIpAddress.ToString();
            }

            return result;
        }
    }
}