using HelloSoap;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Api.Services
{
    public class HelloService : IHelloService
    {
        private readonly Hello_PortTypeClient _client;
        public static string ServiceName = "Hello";
        private readonly ILogger<HelloService> _logger;

        public HelloService(IHttpMessageHandlerFactory factory, ILogger<HelloService> logger)
        {
            _logger = logger;
            _client = new Hello_PortTypeClient();
            _client.Endpoint.EndpointBehaviors.Add(new HttpMessageHandlerBehavior(factory, ServiceName));
        }

        public async Task<string> SayHello(string firstName)
        {
            bool success = false;
            var channel = _client.ChannelFactory.CreateChannel();
            try
            {
                var result = await channel.sayHelloAsync(new sayHelloRequest(firstName));
                
                (channel as IClientChannel).Close();
                success = true;

                return result.greeting;
            }
            catch (FaultException e)
            {
                _logger.LogError(e, "FaultException");
                throw;
            }
            catch (CommunicationException e)
            {
                _logger.LogError(e, "CommunicationException");
                throw;
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "TimeoutException");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception");
                throw;
            }
            finally
            {
                if (!success)
                {
                    (channel as IClientChannel)?.Abort();
                }
            }
        }
    }
}
