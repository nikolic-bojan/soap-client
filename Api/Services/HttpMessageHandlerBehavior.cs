using System;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Api.Services
{
    public class HttpMessageHandlerBehavior : IEndpointBehavior
    {
        private readonly Func<HttpMessageHandler> _httpMessageHandler;

        public HttpMessageHandlerBehavior(IHttpMessageHandlerFactory factory, string serviceName)
        {
            // Here we prescribe how handler will be created.
            // Since it uses IHttpMessageHandlerFactory, this factory will manage the setup and lifetime of the handler, 
            // based on the configuration we provided with AddHttpClient(serviceName) 
            _httpMessageHandler = () => factory.CreateHandler(serviceName);
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // We need this line to add our HttpMessageHandler as HttpClientHandler.
            bindingParameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(handler => _httpMessageHandler()));
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }        

    }

}
