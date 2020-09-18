using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Tests
{
    [TestClass]
    public class HelloServiceUnitTests
    {
        private static ServiceProvider _serviceProvider;
        private static WireMockServer stub;
        
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(Substitute.For<IHttpContextAccessor>());
            services.AddHttpClient(HelloService.ServiceName);

            _serviceProvider = services.BuildServiceProvider();

            string baseUrl = "http://localhost:8088";
            stub = WireMockServer.Start(new WireMockServerSettings
            {
                Urls = new[] { baseUrl },
                ReadStaticMappings = true
            });
        }

        [TestMethod]
        public async Task SayHello_AOK()
        {
            var client = GetClient();            
            stub.
                Given(Request.Create().WithUrl("http://localhost:8088/mockHello_Binding").WithBody(new XPathMatcher("//firstName='Test'")).UsingPost()).
                RespondWith(Response.Create().WithStatusCode(200).WithBody(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <greeting>Hi Test</greeting>
                   </soapenv:Body>
                </soapenv:Envelope>")
                .WithHeader(HeaderNames.ContentType, System.Net.Mime.MediaTypeNames.Text.Xml));


            var result = await client.SayHello("Test");

            Assert.AreEqual("Hi Test", result);
        }

        private static HelloService GetClient()
        {
            var options = Substitute.For<IOptionsMonitor<HelloServiceOptions>>();
            options.CurrentValue.Returns(new HelloServiceOptions());
            return new HelloService(_serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>(), Substitute.For<ILogger<HelloService>>(), options);
        }

        [TestMethod]
        [ExpectedException(typeof(ServiceException))]
        public async Task SayHello_Fail_FaultException()
        {
            var logger = Substitute.For<ILogger<HelloService>>();
            var client = GetClient();

            stub.
                Given(Request.Create().WithUrl("http://localhost:8088/mockHello_Binding").WithBody(new XPathMatcher("//firstName='ERR_01'")).UsingPost()).
                RespondWith(Response.Create().WithStatusCode(500).WithBody(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soapenv:Body>
                    <soapenv:Fault>
                      <faultcode>server</faultcode>
                      <faultstring>error</faultstring>
                      <detail>
                        some error
                      </detail>
                    </soapenv:Fault>
                  </soapenv:Body>
                </soapenv:Envelope>")
                .WithHeader(HeaderNames.ContentType, System.Net.Mime.MediaTypeNames.Text.Xml));


            var result = await client.SayHello("ERR_01");
        }

        [TestMethod]
        [ExpectedException(typeof(ServiceException))]
        public async Task SayHello_Fail_CommunicationException()
        {
            var client = GetClient();

            stub.
                Given(Request.Create().WithUrl("http://localhost:8088/mockHello_Binding").WithBody(new XPathMatcher("//firstName='ERR_02'")).UsingPost()).
                RespondWith(Response.Create().WithStatusCode(503).WithBody(@"some error")
                .WithHeader(HeaderNames.ContentType, System.Net.Mime.MediaTypeNames.Text.Xml));


            var result = await client.SayHello("ERR_02");
        }

        [TestMethod]
        [ExpectedException(typeof(ServiceException))]
        public async Task SayHello_Fail_TimeoutException()
        {
            var client = GetClient();

            stub.
                Given(Request.Create().WithUrl("http://localhost:8088/mockHello_Binding").WithBody(new XPathMatcher("//firstName='ERR_03'")).UsingPost()).
                RespondWith(Response.Create().WithDelay(6000).WithStatusCode(200).WithBody(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <greeting>Hi Test</greeting>
                   </soapenv:Body>
                </soapenv:Envelope>")
                .WithHeader(HeaderNames.ContentType, System.Net.Mime.MediaTypeNames.Text.Xml));


            var result = await client.SayHello("ERR_03");
        }

        [ClassCleanup]
        public static void TearDown()
        {
            stub.Stop();
        }
    }
}
