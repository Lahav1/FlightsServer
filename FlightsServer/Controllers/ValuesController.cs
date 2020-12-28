using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FlightsServer.Models;

namespace FlightsServer.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        public string Get(string src, string dest, int y, int m, int d, int tickets)
        {
            DatabaseHandler dh = new DatabaseHandler();
            QueryDispatcher qd = new QueryDispatcher(dh);
            //var flights = qd.searchflights("haax", "rctp", new datetime(2020, 2, 2), 1);
            var flights = qd.SearchFlights(src, dest, new DateTime(y, m, d), tickets);

            return flights;
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
