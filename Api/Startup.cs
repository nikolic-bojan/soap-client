using System;
using System.Linq;
using Api.Handlers;
using Api.Services;
using Gelf.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // I need this to log to Seq via GELF, but not needed for this sample.
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddGelf(config =>
                {
                    config.LogSource = Environment.MachineName;
                });
            });

            services.Configure<HelloServiceOptions>(Configuration.GetSection("Services:Hello"));

            services.AddHttpContextAccessor();
            services.AddControllers();

            // This header can be propagated to 3rd party services. It is your choice if you want to add it on HttpClient or not.
            services.AddHeaderPropagation(config =>
            {
                config.Headers.Add("x-correlationid", item => Guid.NewGuid().ToString()); // we create a new Guid value if it doesn't exist
            });

            // We can have this as singleton, since we anyways want to initialize client class once and add endpoint behavior (check its constructor).
            services.AddSingleton<IHelloService, HelloService>();

            // Here we configure how the HttpClient with HtpMessagehandler will be configured, like for any HTTP client (e.g. calling REST/JSON service)
            services.AddHttpClient(HelloService.ServiceName, config =>
                {
                    // Some custom configuration like request timeout
                    // config.Timeout = TimeSpan.FromSeconds(5);
                    // WARNING: Setting timeout like this will not work. You need to setup timeouts on your binding!!!
                })
                .AddTraceLogHandler((response) =>
                {
                    // Here we setup that if Response status code is not 200-299, we should log entire HttpClient request and response to 3rd party service.
                    // You can setup any condition based on the HttpResponseMessage.

                    if (!response.IsSuccessStatusCode) return false;

                    if (response.Content == null) return false;

                    // This is a bit more complicated condition, separated in two checks, since we had situations that 
                    // 3rd party service returns some non-XML content (HTML) with status 200, that actually indicates an error
                    if (!response.Content.Headers.Contains(HeaderNames.ContentType)) return false;

                    return !response.Content.Headers.GetValues(HeaderNames.ContentType).Any(p => p.Contains(System.Net.Mime.MediaTypeNames.Text.Xml));
                })
                .AddHeaderPropagation();

            // This is needed to forward headers properly
            // https://devblogs.microsoft.com/aspnet/forwarded-headers-middleware-updates-in-net-core-3-0-preview-6/
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHeaderPropagation();
            app.UseForwardedHeaders();
            app.UseStatusCodePages();

            app.UseRouting();

            app.UseAuthorization();

            // VERY IMPORTANT: Add the following two middleware in this specific order and between UseRouting and UseEndpoints.
            app.UseMiddleware<TraceContextMiddleware>();
            app.UseExceptionHandler(errorApp => errorApp.Run(ExceptionHandler.CreateExceptionHandler(env)));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
