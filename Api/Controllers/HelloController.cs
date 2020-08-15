using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]/{firstName}")]
    public class HelloController : ControllerBase
    {
        private readonly IHelloService _service;

        public HelloController(IHelloService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string firstName)
        {
            // This is a "mock", just to trigger BadRequest response
            if (firstName == "400")
            {
                throw new ValidationException("First name must not be 400");
            }

            var result = await _service.SayHello(firstName);

            return Ok(result);
        }
    }
}