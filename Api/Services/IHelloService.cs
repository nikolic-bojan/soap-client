using System.Threading.Tasks;

namespace Api.Services
{
    public interface IHelloService
    {
        Task<string> SayHello(string firstName);
    }
}
