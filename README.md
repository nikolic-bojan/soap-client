# Make SOAP requests using IHttpClientFactory in .NET Core

SOAP services became second class citizen in .NET Core. REST was "the-way-to-go" and who creates SOAP services these days anyways? Well, maybe not these days, but a lot of them were created back in the days when SOAP was the King. Some of them are still alive and you need to access them. 

Sure, accessing is not a big deal, VS still supports this, but if you worked with `IHttpClientFactory` when calling REST services and liked it, you would want to have all that while working with SOAP.

Is this possible?

> TL;DR;
Yes it is! You can find sample application here on my GitHub repository https://github.com/nikolic-bojan/soap-client Browse through the code to figure what I did or just continue reading this article.

# Problem and digging out the solution

Around a year and a half ago we moved our first service from "old" Framework to .NET Core. Later that year I started looking for a way to move to .NET Core some services that call SOAP 3rd party services. I wasn't satisfied with the basic stuff Core offers and I wanted something more similar to the options we have with IHttpClientFactory. Actually, I wanted the same experience!

I started looking for a solution. There were some custom libraries, but that was out of the question. They also felt hacky. I played around writing `ISoapClientFactory` as a copy of HTTP one, but only to realize I do not want to maintain something like that. At last, I postponed this for later.

Few months back, I gave it another go. This time I ran into GitHub issue https://github.com/dotnet/wcf/issues/3230 that pointed me to both this blog post https://medium.com/trueengineering/realization-of-the-connections-pool-with-wcf-for-net-core-with-usage-of-httpclientfactory-c2cb2676423e and this PR added to Core https://github.com/dotnet/wcf/pull/2534/files by a demigod of WCF - Matt Connew. Big *thank you* for these people!

So, is this article more/less a re-chewing of the GitHub Issue, PR and the blog post? Yes, you can say that, but I will explain one or two other walls I hit with this, so it is up to you where you will continue reading.

## Add SOAP service

Adding existing SOAP/WCF service should not be a big issue. You should just follow the official documentation here https://docs.microsoft.com/en-us/dotnet/core/additional-tools/wcf-web-service-reference-guide

## Caveat #1

What I encountered is that I couldn't do it. I kept getting the error that tool can't work on my .NET Core 3.x (I removed all previous versions) and requires v2.1. In order not to be blocked, I managed to find and install v2.1. Runtime installation should do the trick (I am not 100% sure, it was 2 months ago, do not hunt me down if runtime doesn't work and you need SDK). Visual Studio was now creating proxy as it should. Great!

## How to make SOAP client work with IHttpClientFactory

When I went through those GitHub issue and PR, it is quite simple to answer - you need to add new Endpoint Behavior that will basically replace the default `HttpClientHandler` with the `HttpMessageHandler` that will be created by our beloved `IHttpMessageHandlerFactory`.

OK, how do I do that? First, you will need a new class that will implement `IEndpointBehavior` and will look like this

```csharp
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
```

There you have all the "magic" where constructor accepts `IHttpMessageHandlerFactory` and defines function for creating a new handler; plus the part where we define how newly created `HttpMessageHandler` should be used as `HttpClientHandler` when calling SOAP service.

Of course, I had to add this EndpointBehavior in constructor of my `HelloService` singleton that injects `IHttpMessageHandlerFactory`

```csharp
public HelloService(IHttpMessageHandlerFactory factory, ILogger<HelloService> logger)
{
    _logger = logger;
    _client = new Hello_PortTypeClient();
    _client.Endpoint.EndpointBehaviors.Add(new HttpMessageHandlerBehavior(factory, ServiceName));
}
```

Wait! What is this *ServiceName*? Well, I need to configure HttpClient, so I will refer it with a *ServiceName* both in `HelloService` class and in `Startup`

```csharp
// Here we configure how the HttpClient with HtpMessagehandler will be configured, like for any HTTP client (e.g. calling REST/JSON service)
services.AddHttpClient(HelloService.ServiceName, config =>
{
    // Some custom configuration like request timeout
    config.Timeout = TimeSpan.FromSeconds(5);
});
```

There you will put your additional handlers, Polly resilience and all those nice things, like you already do when calling REST.

## Caveat #2

You can't put in `AddHttpClient` stuff like `BaseAddress` or Authentication etc. That stuff needs to be setup on Endpoint or Binding, like explained in this blog post https://medium.com/grensesnittet/integrating-with-soap-web-services-in-net-core-adebfad173fb

I suggest you setup some of the things in constructor of your service (HelloService in my example) with injecting of Options, but I will leave that up to you, depending on how much things you need to setup and your best practices on that. 

## Caveat #3

I was so happy with the solution, so I just wanted to confirm it like in blog post with some load test with JMeter. I started the SoapUI mock (provided in the repository), opened CurrPorts and I didn't like what I found there. Even though calls did work, application opened too many connections - no connection pooling! Basically opened a connection per request! That was unacceptable.

It looked like solution was not using `IHttpClientFactory`. That was not possible, I went through debugger, it should work as advertised. CurrPorts was looking good on that blog post. What am I missing?

After few hours wasted on looking what could be wrong, I noticed that there are new NuGet versions available for `System.ServiceModel.*` packages. Mine (generated by VS) were `4.4.0`, latest were `4.7.0`. No, it can't be something stupid like that... yes it was.

I updated NuGet packages and all started working as it should!

I hit it from JMeter with 5 threads (and 100 calls per thread) and *voila* - 5 connections opened, just like it should!

![Alt Text](https://dev-to-uploads.s3.amazonaws.com/i/6zuv8jz3ymg3ym4s343b.PNG)

## Final words

If you would like to recreate code from repository, you will need:
- SoapUI, to run mock (https://www.soapui.org/downloads/soapui/)
- JMeter to run load tests (https://jmeter.apache.org/download_jmeter.cgi)
- CurrPorts to see how many ports have been open (https://www.nirsoft.net/utils/cports.html#DownloadLinks)

I wanted to keep this short (failed as usual), so please ask questions if you are not clear on some of the steps.
