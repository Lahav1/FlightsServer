﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using FlightsServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Http.Cors;

namespace FlightsServer.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ValuesController : ApiController
    {
        static string configFilePath = @"C:\Users\USER\Desktop\config.json";

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
            DatabaseHandler dh = new DatabaseHandler(configFilePath);
            QueryDispatcher qd = new QueryDispatcher(dh, configFilePath);
            string flights = qd.SearchFlights(src, dest, new DateTime(y, m, d), tickets);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(flights, Encoding.UTF8, "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }


        /// <summary>
        /// The function is used for auto completion of airport's names.
        /// </summary>
        /// <param name="airportName">The substring of the airport name.</param>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A JSON string of all the airports matching the substring.</returns>
        public HttpResponseMessage Get(string airportName, HttpRequestMessage request)
        {
            var response = this.Request.CreateResponse(HttpStatusCode.BadRequest);
            DatabaseHandler dh = new DatabaseHandler(configFilePath);
            QueryDispatcher qd = new QueryDispatcher(dh, configFilePath);
            string airports = qd.FindAirport(airportName);
            response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(airports, Encoding.UTF8, "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");



            return response;

        }

        // POST api/values
        public HttpResponseMessage Post([FromBody] JToken postData, HttpRequestMessage request)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(postData.ToString());
            DataTable responseObj = new DataTable();
            string json = string.Empty;
            json = JsonConvert.SerializeObject(responseObj);
            DatabaseHandler dh = new DatabaseHandler(configFilePath);
            QueryDispatcher qd = new QueryDispatcher(dh, configFilePath);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);

            try
            {
                if (request.RequestUri.AbsolutePath == "/api/Values/cancel_reservation")
                {

                    qd.CancelReservation(result["reservation_id"].ToString());
                    response.Content = new StringContent("", Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/make_reservation")
                {
                    response = qd.CreateNewReservation(result["email"].ToString(), JsonConvert.DeserializeObject<List<string>>(result["flights"].ToString()), Convert.ToInt32(result["number_of_tickets"]));
                    response.Content = new StringContent("", Encoding.UTF8, "application/json");
                    return response;
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/user_reservations")
                {
                    response.Content = new StringContent(qd.FindUserReservations(result["user_id"].ToString()), Encoding.UTF8, "application/json");                    
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/add_flight")
                {
                    return qd.AddFlight(result["route_id"].ToString(), result["departure_time_GMT"].ToString(), result["arrival_time_GMT"].ToString(),
                        Convert.ToInt32(result["ticket_price"].ToString()), result["airplane_IATA"].ToString());
                }
                else if(request.RequestUri.AbsolutePath == "/api/Values/cancel_flight")
                {
                    return qd.RemoveFlight(result["flight_id"].ToString());
                }
                else if(request.RequestUri.AbsolutePath == "/api/Values/add_route")
                {
                    return qd.AddRoute(result["source_airport"].ToString(), result["destination_airport"].ToString(), result["airline_id"].ToString(), result["equipment"].ToString());
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/remove_route")
                {
                    return qd.RemoveRoute(result["route_id"].ToString());
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/add_airport")
                {
                    return qd.AddAirport(result["name"].ToString(), result["city"].ToString(), result["country"].ToString(),
                        result["IATA"].ToString(), result["ICAO"].ToString(), Convert.ToDouble(result["lat"].ToString()),
                        Convert.ToDouble(result["lon"].ToString()), Convert.ToDouble(result["timezone"].ToString()));
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/remove_airport")
                {
                    return qd.RemoveAirport(result["airport_id"].ToString());
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/add_airplane")
                {
                    return qd.AddAirplane(result["name"].ToString(), result["IATA"].ToString(), result["ICAO"].ToString(),
                        Convert.ToInt32(result["cruise_speed"].ToString()), Convert.ToInt32(result["num_of_seats"].ToString()));
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/remove_airplane")
                {
                    return qd.RemoveAirplane(result["IATA"].ToString());
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/add_airline")
                {
                    return qd.AddAirline(result["name"].ToString(), result["IATA"].ToString(), result["ICAO"].ToString(), 
                        bool.Parse(result["is_active"].ToString()), Convert.ToInt32(result["rating"].ToString()));
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/remove_airline")
                {
                    return qd.RemoveAirline(result["id"].ToString());
                }
                else if (request.RequestUri.AbsolutePath == "/api/Values/sign_up")
                {
                    return qd.SignUp(result["email"].ToString(), result["full_name"].ToString(), DateTime.Parse(result["DOB"].ToString()), result["passport_id"].ToString());
                }
                else if(request.RequestUri.AbsolutePath == "/api/Values/is_admin")
                {
                    response.Content = new StringContent(qd.IsAdmin(result["email"].ToString(), result["password"].ToString()), Encoding.UTF8, "application/json");
                }
                

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                response.StatusCode = HttpStatusCode.BadRequest;
            }
            response.Headers.Add("Access-Control-Allow-Origin", "*");
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
