using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MqMessages;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FibonacciController : ControllerBase
    {
        private readonly ILogger<FibonacciController> log;
        private readonly IBus bus;

        public FibonacciController(ILogger<FibonacciController> logger, IBus bus)
        {
            log = logger;
            this.bus = bus;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(500, "Client must do POST request.");
        }
        
        public int NaiveFib(int n) => n switch {
            0 => 0,
            1 => 1,
            _ => NaiveFib(n-2) + NaiveFib(n-1)
        };

        [HttpPost]
        public IActionResult Post([FromQuery] int n)
        {
            var calculationTask = Task.Run(() => {
                var value = NaiveFib(n);
                var msg = new FibonnaciValue(n, value);
                log.LogInformation($"Sending fib({n}) -> {value}");
                bus.PubSub.PublishAsync(msg);
            });
            calculationTask.ConfigureAwait(false);
            
            var msg = $"Requested fib({n})";
            log.LogInformation(msg);
            return StatusCode(200, msg);
        }
    }
}
