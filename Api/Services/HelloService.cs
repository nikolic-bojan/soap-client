using HelloSoap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using Api.Helpers;

namespace Api.Services
{
    public class HelloService : IHelloService
    {
        private readonly Hello_PortTypeClient _client;
        public static string ServiceName = "Hello";
        private readonly ILogger<HelloService> _logger;

        public HelloService(IHttpMessageHandlerFactory factory, ILogger<HelloService> logger, IOptionsMonitor<HelloServiceOptions> options)
        {
            _logger = logger;
            _client = new Hello_PortTypeClient();
            _client.Endpoint.EndpointBehaviors.Add(new HttpMessageHandlerBehavior(factory, ServiceName));
            _client.Endpoint.Address = new EndpointAddress(options.CurrentValue.EndpointAddress);

            _client.Endpoint.Binding.CloseTimeout = TimeSpan.FromSeconds(options.CurrentValue.TimeoutSeconds);
            _client.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(options.CurrentValue.TimeoutSeconds);
            _client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(options.CurrentValue.TimeoutSeconds);
            _client.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(options.CurrentValue.TimeoutSeconds);
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
                throw new ServiceException(e.Message, e);
            }
            catch (CommunicationException e)
            {
                throw new ServiceException(e.Message, e);
            }
            catch (TimeoutException e)
            {
                throw new ServiceException(e.Message, e);
            }
            catch (Exception e)
            {
                throw new ServiceException(e.Message, e);
            }
            finally
            {
                if (!success)
                {
                    (channel as IClientChannel)?.Abort();
                }
            }
        }

        /* 
         * Alternatively, we can abstract common SOAP logic to a separate helper method and 
         * have it wrap around our business logic, like so:
         */
        public async Task<string> SayHelloWithHelper(string firstName)
        {
            return await SoapHelper.IssueSoapCallAsync(_client, async (channel) =>
            {
                var result = await channel.sayHelloAsync(new sayHelloRequest(firstName));

                return result.greeting;
            }, _logger);
        }
    }
}
