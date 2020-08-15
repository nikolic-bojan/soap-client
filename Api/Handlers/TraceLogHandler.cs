using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Handlers
{
    internal class TraceLogHandler : DelegatingHandler
    {
        public const string TraceMe = "traceme";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<HttpResponseMessage, bool> _shouldLog;

        public TraceLogHandler(IHttpContextAccessor httpContextAccessor, Func<HttpResponseMessage, bool> shouldLog)
        {
            _httpContextAccessor = httpContextAccessor;
            _shouldLog = shouldLog;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool logPayloads = false;

            // If you pass a query string parameter "traceme", HttpClient request/response will be logged.
            bool traceMe = _httpContextAccessor.HttpContext.Request.Query.ContainsKey(TraceMe);

            logPayloads = logPayloads || traceMe;

            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                // We run the ShouldLog function that calculates, based on HttpResponseMessage, if we should log HttpClient request/response.
                logPayloads = logPayloads || _shouldLog(response);
            }
            catch (Exception)
            {
                // We want to log HttpClient request/response when some exception occurs, so we can reproduce what caused it.
                logPayloads = true;
                throw;
            }
            finally
            {
                // Finally, we check if we decided to log HttpClient request/response or not.
                // Only if we want to, we will have some allocations for the logger and try to read headers and contents.
                if (logPayloads)
                {
                    var logger = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<ILogger<TraceLogHandler>>();
                    Dictionary<string, object> scope = new Dictionary<string, object>();

                    scope.TryAdd("Service_RequestHeaders", request);
                    if (request?.Content != null)
                    {
                        scope.Add("Service_RequestBody", await request.Content.ReadAsStringAsync());
                    }
                    scope.TryAdd("Service_ResponseHeaders", response);
                    if (response?.Content != null)
                    {
                        scope.Add("Service_ResponseBody", await response.Content.ReadAsStringAsync());
                    }
                    using (logger.BeginScope(scope))
                    {
                        logger.LogInformation("[TRACE] Service Request/Response");
                    }
                }
            }

            return response;
        }
    }
}