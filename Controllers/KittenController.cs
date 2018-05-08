using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CfSampleAppDotNetCore.Models;

namespace CfSampleAppDotNetCore.Controllers
{
    [Route("[controller]")]
    public class KittenController : Controller
    {
        public KittenController(IKittenRepository kittens)
        {
            Kittens = kittens;
        }
        public IKittenRepository Kittens { get; set; }

        [HttpGet]
        public List<string>  Find()
        {
            return Kittens.Find();
        }

        [HttpPost]
        public IActionResult Create([FromBody] Kitten kitten)
        {
            if (kitten == null)
            {
                return BadRequest();
            }
            Kittens.Create(kitten);
            return StatusCode(201);
        }
    }
}
