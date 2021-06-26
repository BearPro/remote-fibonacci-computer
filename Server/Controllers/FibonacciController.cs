using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Common;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FibonacciController : ControllerBase
    {
        private readonly ILogger<FibonacciController> logger;
        private readonly IBus bus;
        private readonly FibonacciComputerService fibonacciComputer;

        public FibonacciController(
            ILogger<FibonacciController> logger, 
            IBus bus, 
            FibonacciComputerService fibonacciComputer)
        {
            this.logger = logger;
            this.bus = bus;
            this.fibonacciComputer = fibonacciComputer;
        }

        [HttpPost]
        public IActionResult Post(FibonnaciValue current)
        {
            var calculationTask = Task.Run(() => {
                var next = fibonacciComputer.ComputeNext(current);

                logger.LogInformation($"Sending fib({next.n}) -> {next.value}");
                
                bus.PubSub.PublishAsync(next);
            });
            
            calculationTask.ConfigureAwait(false);
            var msg = $"Requested fib({current.n + 1})";
            logger.LogInformation(msg);
            return StatusCode(200, msg);
        }
    }
}
