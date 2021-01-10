using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyLittleUrlAPI.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MyLittleUrlAPI.Controllers
{
    [Route("api/littleurl")]
    public class LittleUrlController : Controller
    {
        // private LittleUrlContext _littleUrlContext;

        private LittleUrlMongoContext _littleUrlMongoContext;
        private int _nextUrlId;

        //public LittleUrlController(LittleUrlContext context)
        //{
        //    _littleUrlContext = context;
        //
        //    if(_littleUrlContext.littleUrlList.Count() == 0)
        //    {
        //        // Seed values
        //        _littleUrlContext.littleUrlList.Add(new LittleUrl { ShortUrl = "abc", LongUrl = "ABC_somelongurlcodedas_abc" });
        //        _littleUrlContext.littleUrlList.Add(new LittleUrl { ShortUrl = "xyz", LongUrl = "ABC_somelongurlcodedas_xyz" });
        //        _littleUrlContext.littleUrlList.Add(new LittleUrl { ShortUrl = "123", LongUrl = "ABC_somelongurlcodedas_123" });
        //
        //        _littleUrlContext.SaveChanges();
        //    }
        //}

        public LittleUrlController()
        {
            _littleUrlMongoContext = new LittleUrlMongoContext();
            _nextUrlId = -1;
        }

        // API Methods
        // Route: api/littleurl
        [HttpGet]
        public IEnumerable<LittleUrl> GetUrls()
        {
            HttpRequest req = Request;
            if (IsUnauthorizedCaller(req))
                throw new UnauthorizedAccessException();

            // return _littleUrlContext.littleUrlList;
            return _littleUrlMongoContext.littleUrlList;
        }

        // Route: api/littleurl/<key>
        [HttpGet("{key}", Name = "GetByKey")]
        public IActionResult GetByKey(string key)
        {
            HttpRequest req = Request;
            if (IsUnauthorizedCaller(req))
                return Unauthorized();

            if (key.Length == 0)
                return BadRequest("Key value required.");

            // LittleUrl item = _littleUrlContext.littleUrlList.FirstOrDefault(url => url.ShortUrl == key.ToLower());
            LittleUrl item = _littleUrlMongoContext.GetUrl(key.ToLower(), false);
            if (item == null)
                return NotFound("URL does not exist.");

            return Ok(item);
        }

        // Route: api/littleurl
        [HttpPost]
        public IActionResult AddToList([FromBody] LittleUrl lUrl)
        {
            HttpRequest req = Request;
            if (IsUnauthorizedCaller(req))
                return Unauthorized();

            if (lUrl.LongUrl.Length == 0)
                return BadRequest("URL value is required.");

            // Check if the URL already exists
            LittleUrl item;
            // item = _littleUrlContext.littleUrlList.FirstOrDefault(url => url.LongUrl == lUrl.LongUrl.ToLower());

            item = _littleUrlMongoContext.CheckUrl(lUrl.LongUrl);

            if (item == null)
            {
                // create
                item = new LittleUrl { UrlId = GetNextId(), LongUrl = lUrl.LongUrl, ShortUrl = GetNewKey() };
                //_littleUrlContext.littleUrlList.Add(item);
                //_littleUrlContext.SaveChanges();

                _littleUrlMongoContext.InsertUrl(item);
            }

            // return created/found item
            return CreatedAtRoute("GetByKey", new { key = item.ShortUrl }, item);
        }

        // Route: api/littleurl
        [HttpDelete ("{key}")]
        public IActionResult Delete(string key)
        {
            HttpRequest req = Request;
            if (IsUnauthorizedCaller(req))
                return Unauthorized();

            if (key.Length == 0)
                return BadRequest("URL value is required.");

            // Check if the URL exists
            LittleUrl item = _littleUrlMongoContext.GetUrl(key.ToLower(), false);
            if (item == null)
                return NotFound("URL does not exist.");

            // Found document; Delete
            item = _littleUrlMongoContext.ToggleDelete(key.ToLower(), true);

            // return deleted item
            return Ok(item);
        }

        // Route: api/littleurl
        [HttpPost("{key}")]
        public IActionResult UnDelete(string key)
        {
            HttpRequest req = Request;
            if (IsUnauthorizedCaller(req))
                return Unauthorized();

            if (key.Length == 0)
                return BadRequest("URL value is required.");

            // Check if the deleted URL exists
            LittleUrl item = _littleUrlMongoContext.GetUrl(key.ToLower(), true);
            if (item == null)
                return NotFound("URL does not exist.");

            // Found document; UnDelete
            item = _littleUrlMongoContext.ToggleDelete(key.ToLower(), false);

            // return deleted item
            return Ok(item);
        }

        // Private helper
        private string GetNewKey()
        {
            string sNewKey;
            LittleUrl item;

            byte[] b = new byte[3];
            Random rnd = new Random();
            Regex rx = new Regex(@"([A-Za-z0-9]){3}");

            do
            {
                do
                {
                    rnd.NextBytes(b);
                    sNewKey = Convert.ToBase64String(b).Substring(0, 3);
                } while (!rx.IsMatch(sNewKey));

                // Find new key in list (irrespective of delete flag)
                item = _littleUrlMongoContext.littleUrlList
                            .FirstOrDefault(url => url.ShortUrl == sNewKey.ToLower());
                // item = _littleUrlMongoContext.GetUrl(sNewKey.ToLower());
            } while (item != null);

            return sNewKey.ToLower();
        }

        private int GetNextId()
        {
            if (_nextUrlId < 0)
                _nextUrlId = _littleUrlMongoContext.GetMaxId();
            
            return ++_nextUrlId;
        }

        private bool IsUnauthorizedCaller(HttpRequest req)
        {
            // Read from config file
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            // Read purge window from config; if not valid, use 90 as default
            string clientSecret = configBuilder.Build().GetValue<string>("Security:ClientSecret");
            return (req.Headers["Client-Secret"].FirstOrDefault() != clientSecret);
        }
    }
}
