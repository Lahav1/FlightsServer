using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using FlightsServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public HttpResponseMessage Get(string src, string dest, int y, int m, int d, int tickets)
        {
            DatabaseHandler dh = new DatabaseHandler(@"C:\Users\USER\Desktop\config.json");
            QueryDispatcher qd = new QueryDispatcher(dh);
            string flights = qd.SearchFlights(src, dest, new DateTime(y, m, d), tickets);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(flights, Encoding.UTF8, "application/json");
            return response;
        }


        // POST api/values
        public HttpResponseMessage Post([FromBody] JToken postData, HttpRequestMessage request)
        {


            JObject result = (JObject)JsonConvert.DeserializeObject(postData.ToString()); 
            DataTable responseObj = new DataTable();
            string json = string.Empty;
            json = JsonConvert.SerializeObject(responseObj);
            DatabaseHandler dh = new DatabaseHandler(@"C:\Users\USER\Desktop\config.json");
            QueryDispatcher qd = new QueryDispatcher(dh);

            qd.CreateNewReservation(result["email"].ToString(), JsonConvert.DeserializeObject<List<string>>(result["flights"].ToString()), Convert.ToInt32(result["number_of_tickets"]));

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("", Encoding.UTF8, "application/json");
            return response;

        }

        public HttpResponseMessage Post(string omer)
        {


            HttpResponseMessage response = null;
            
            response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
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
