using System;
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


        /// <summary>
        /// The function creates a new reservation from a list of flights.
        /// </summary>
        /// <param name="userID">The user that wants to order the flights.</param>
        /// <param name="flights">The list of all the flights.</param>
        /// <param name="numberOfTickets">Number of tickets the user want to purchase.</param>
        public void CreateNewReservation(string userID, List<string> flights, int numberOfTickets)
        {
            string maxID = dbh.ExecuteQuery($"SELECT MAX(id) FROM reservation;").Item2[0][0] + 1; // Gets the next available id for the table.
            List<string> queries = new List<string>();
            foreach (var flight in flights)
            {
                queries.Add($"CALL OrderFlight('{maxID}', '{userID}', '{flight}', {numberOfTickets});");
            }
            dbh.ExecuteNonQuery(queries);
        }


        /// <summary>
        /// The function cancels an existing reservation from the database.
        /// </summary>
        /// <param name="reservationID">The reservation's ID.</param>
        public void CancelReservation(string reservationID)
        {
            List<string> queries = new List<string>
            {
                $"CALL CancelReservation('{reservationID}');"
            };
            dbh.ExecuteNonQuery(queries);
        }


        public string FindUserReservations(string userID)
        {
            string query = $"CALL FindUserReservations('{userID}');";
            Tuple<Dictionary<string, int>, List<List<string>>> reservations = dbh.ExecuteQuery(query);

            var headers = reservations.Item1;
            var tableValues = reservations.Item2;

            // Create JSON of reservations.
            JArray reservationsResults = new JArray();

            int i = 0;
            var lastReservationID = tableValues[i][headers["reservation_id"]];
            var nextReservationID = tableValues[i][headers["reservation_id"]];
            while (i < tableValues.Count)
            {
                dynamic reservationObj = new JObject();
                reservationObj.reservation_id = tableValues[i][headers["reservation_id"]];
                reservationObj.number_of_passangers = Convert.ToInt32(tableValues[i][headers["number_of_passangers"]]);
                reservationObj.price = 0;

                JArray reservationFlights = new JArray();
                while (i < tableValues.Count)
                {
                    if(lastReservationID != nextReservationID)
                    {
                        lastReservationID = nextReservationID;
                        break;
                    }
                    else
                    {
                        dynamic flight = new JObject();
                        flight.flight_id = tableValues[i][headers["flight_id"]];
                        flight.airline = tableValues[i][headers["airline"]];
                        flight.flight_number = tableValues[i][headers["flight_number"]];
                        flight.airplane = tableValues[i][headers["airplane"]];
                        flight.airplane = tableValues[i][headers["airplane"]];
                        flight.local_departure_time = tableValues[i][headers["local_departure_time"]];
                        flight.local_arrival_time = tableValues[i][headers["local_arrival_time"]];

                        dynamic source_airport = new JObject();
                        source_airport.name = tableValues[i][headers["source_airport_name"]];
                        source_airport.city = tableValues[i][headers["source_airport_city"]];
                        source_airport.country = tableValues[i][headers["source_airport_country"]];
                        source_airport.latitude = tableValues[i][headers["source_airport_latitude"]];
                        source_airport.longitude = tableValues[i][headers["source_airport_longitude"]];
                        flight.source_airport = source_airport;

                        dynamic destination_airport = new JObject();
                        destination_airport.name = tableValues[i][headers["destination_airport_name"]];
                        destination_airport.city = tableValues[i][headers["destination_airport_city"]];
                        destination_airport.country = tableValues[i][headers["destination_airport_country"]];
                        destination_airport.latitude = tableValues[i][headers["destination_airport_latitude"]];
                        destination_airport.longitude = tableValues[i][headers["destination_airport_longitude"]];
                        flight.destination_airport = destination_airport;

                        reservationObj.price += Convert.ToInt32(tableValues[i][headers["ticket_price"]]) *
                            Convert.ToInt32(tableValues[i][headers["number_of_passangers"]]);

                        reservationFlights.Add(flight);

                        if(i < tableValues.Count - 1)
                        {
                            nextReservationID = tableValues[i + 1][headers["reservation_id"]];
                        }
                        i++;
                    }
                }
                reservationObj.fiights = reservationFlights;
                reservationsResults.Add(reservationObj);
            }
            return reservationsResults.ToString();

        }
    }
}
