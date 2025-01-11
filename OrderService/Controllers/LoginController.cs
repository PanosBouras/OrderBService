using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers
{
    public class Credentials
    {
        public string Username { get; set; }
        public decimal Password { get; set; }
    }

    [ApiController]
    [Route("[controller]")]

    public class LoginController : Controller
    {
        [HttpGet(Name = "PostLogin")]
        public async Task<string> Login([FromQuery] string username, [FromQuery] string password)
        {
            if (username == "panos" && password == "123")
            {
                return "true";
            }
            return "false";
        }

    }
}
