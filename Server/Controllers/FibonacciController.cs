using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FibonacciController : ControllerBase
    {
        private readonly ILogger<FibonacciController> _logger;
        private readonly IBus bus;

        public FibonacciController(ILogger<FibonacciController> logger, IBus bus)
        {
            _logger = logger;
            this.bus = bus;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(500, "Client must do POST request.");
        }
        
        [HttpPost]
        public IActionResult Post([FromQuery] int n)
        {
            return StatusCode(200, $"fib({n})");
        }
    }
}
