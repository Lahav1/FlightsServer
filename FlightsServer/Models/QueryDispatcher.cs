﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FlightsServer.Models
{
    class QueryDispatcher
    {
        /// <summary>
        /// The database handler.
        /// </summary>
        private DatabaseHandler dbh;

        /// <summary>
        /// QueryDispatcher constructor.
        /// </summary>
        /// <param name="dbh">The database handler.</param>
        public QueryDispatcher(DatabaseHandler dbh)
        {
            this.dbh = dbh;
        }


        /// <summary>
        /// The function search for all available flights from source airport to destination airport on a certain date.
        /// </summary>
        /// <param name="sourceICAO">The ICAO code of the source airport.</param>
        /// <param name="destinationICAO">The ICAO code of the destination airport.</param>
        /// <param name="departureTime">The date of departure/</param>
        /// <param name="numberOfTickets">Number of tickets to purchase.</param>
        /// <returns>A JSON object of all the available flights.</returns>
        public string SearchFlights(string sourceICAO, string destinationICAO, DateTime departureTime, int numberOfTickets)
        {
            const short MAX_CONNECTING_FLIGHTS = 3; // Notice that this is determined in the SQL procedure.
            string query = $"CALL FindFlights('{sourceICAO}','{destinationICAO}','{departureTime:yyyy-MM-dd}',{numberOfTickets});";
            Tuple<Dictionary<string, int>, List<List<string>>> flightsList = dbh.ExecuteQuery(query);

            var headers = flightsList.Item1;
            var tableValues = flightsList.Item2;

            // Create JSON of flights.
            JArray flightResults = new JArray();

            foreach (var trip in tableValues)
            {
                JArray tripFlights = new JArray();
                dynamic tripObj = new JObject();
                for (int i = 1; i <= MAX_CONNECTING_FLIGHTS; i++)
                {
                    if (trip[headers[$"flight{i}_id"]] != String.Empty)
                    {
                        dynamic flight = new JObject();
                        flight.flight_ID = trip[headers[$"flight{i}_id"]];
                        flight.flight_number = trip[headers[$"flight{i}_flight_number"]];
                        flight.local_departure_time = trip[headers[$"flight{i}_departure_time_local"]];
                        flight.local_arrival_time = trip[headers[$"flight{i}_arrival_time_local"]];

                        dynamic airline = new JObject();
                        airline.name = trip[headers[$"flight{i}_airline_name"]];
                        airline.rating = trip[headers[$"flight{i}_airline_rating"]];
                        flight.airline = airline;

                        flight.airplane = trip[headers[$"flight{i}_airplane"]];

                        dynamic srcAirport = new JObject();
                        srcAirport.name = trip[headers[$"leg{i}_src_airport_name"]];
                        srcAirport.city = trip[headers[$"leg{i}_src_airport_city"]];
                        srcAirport.country = trip[headers[$"leg{i}_src_airport_country"]];
                        srcAirport.latitude = trip[headers[$"leg{i}_src_airport_lat"]];
                        srcAirport.longitude = trip[headers[$"leg{i}_src_airport_lon"]];
                        flight.source_airport = srcAirport;

                        dynamic destAirport = new JObject();
                        destAirport.name = trip[headers[$"leg{i}_dest_airport_name"]];
                        destAirport.city = trip[headers[$"leg{i}_dest_airport_city"]];
                        destAirport.country = trip[headers[$"leg{i}_dest_airport_country"]];
                        destAirport.latitude = trip[headers[$"leg{i}_dest_airport_lat"]];
                        destAirport.longitude = trip[headers[$"leg{i}_dest_airport_lon"]];
                        flight.destination_airport = destAirport;

                        flight.duration = trip[headers[$"flight{i}_duration"]];
                        if (i < MAX_CONNECTING_FLIGHTS && trip[headers[$"leg{i}_connection_duration"]] != String.Empty)
                        {
                            flight.connection_time = trip[headers[$"leg{i}_connection_duration"]];
                        }
                        tripFlights.Add(flight);
                    }
                }
                tripObj.trip_flights = tripFlights;
                tripObj.duration = trip[headers["duration"]];
                tripObj.price = trip[headers["price"]];
                flightResults.Add(tripObj);
            }
            return flightResults.ToString();
        }
    }
}
