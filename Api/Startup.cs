using System;
using Api.Handlers;
using Api.Services;
using Gelf.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            //services.AddLogging(loggingBuilder =>
            //{
            //    loggingBuilder.AddGelf();
            //});

            services.AddHttpContextAccessor();
            services.AddControllers();

            // We can have this as singleton, since we anyways want to initialize client class once and add endpoint behavior (check its constructor).
            services.AddSingleton<IHelloService, HelloService>();

            // Here we configure how the HttpClient with HtpMessagehandler will be configured, like for any HTTP client (e.g. calling REST/JSON service)
            services.AddHttpClient(HelloService.ServiceName, config =>
                {
                    // Some custom configuration like request timeout
                    config.Timeout = TimeSpan.FromSeconds(5);
                })
                .AddTraceLogHandler((response) =>
                {
                    // Here we setup that if Response status code is not 200-299, we should log entire HttpClient request and response to 3rd party service.
                    // You can setup any condition based on the HttpResponseMessage.
                    return !response.IsSuccessStatusCode;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
