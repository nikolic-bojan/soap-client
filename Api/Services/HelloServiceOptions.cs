namespace Api.Services
{
    public class HelloServiceOptions
    {
        public string EndpointAddress { get; set; } = "http://localhost:8088/mockHello_Binding";

        public int TimeoutSeconds { get; set; } = 5;
    }
}
