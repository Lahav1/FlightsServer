using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.ComponentModel.DataAnnotations;

namespace FlightsServer.Models
{
    class QueryDispatcher
    {
        /// <summary>
        /// The email's address.
        /// </summary>
        private string emailAddress;

        /// <summary>
        /// The email's password.
        /// </summary>
        private string emailPassword;

        /// <summary>
        /// The database handler.
        /// </summary>
        private DatabaseHandler dbh;

        /// <summary>
        /// QueryDispatcher constructor.
        /// </summary>
        /// <param name="dbh">The database handler.</param>
        public QueryDispatcher(DatabaseHandler dbh, string configFilePath)
        {
            this.dbh = dbh;
            string config = DatabaseUtils.ReadFile(configFilePath);
            JObject parsedConfig = JObject.Parse(config);
            this.emailAddress = parsedConfig["Server"]["Email"]["address"].ToString();
            this.emailPassword = parsedConfig["Server"]["Email"]["password"].ToString();
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
        /// <param name="userEmail">The user that wants to order the flights.</param>
        /// <param name="flights">The list of all the flights.</param>
        /// <param name="numberOfTickets">Number of tickets the user want to purchase.</param>
        public HttpResponseMessage CreateNewReservation(string userEmail, List<string> flights, int numberOfTickets)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            int maxID = 0;
            try
            {
                maxID = Convert.ToInt32(dbh.ExecuteQuery($"SELECT substr(id, 3) FROM reservation ORDER BY substr(id, 3) * 1 DESC LIMIT 1;")
                    .Item2[0][0]); // Gets the next available id for the table.

            } catch
            {
                // Creating first user.
            }
            if (Convert.ToInt32(dbh.ExecuteQuery($"SELECT COUNT(*) FROM user WHERE email='{userEmail}';").Item2[0][0]) == 0)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            List<string> queries = new List<string>();
            foreach (var flight in flights)
            {
                queries.Add($"CALL OrderFlight('RS{maxID + 1}', '{userEmail}', '{flight}', {numberOfTickets});");
            }
            dbh.ExecuteNonQuery(queries);
            return response;
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

        /// <summary>
        /// The function adds a new airline into the airline table.
        /// </summary>
        /// <param name="name">The airline's full name.</param>
        /// <param name="IATA">The airline's IATA code (2 characters).</param>
        /// <param name="ICAO">The airline's ICAO code (3 characters).</param>
        /// <param name="isActive">If the airline is active or not.</param>
        /// <param name="rating">The airline's rating.</param>
        public HttpResponseMessage AddAirline(string name, string IATA, string ICAO, bool isActive, double rating)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            if (IATA.Length != 2 || ICAO.Length != 3)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            int maxID = Convert.ToInt32(dbh.ExecuteQuery("SELECT substr(id, 3) FROM airline ORDER BY substr(id, 3) * 1 DESC LIMIT 1;").Item2[0][0]);
            List<string> query = new List<string>()
            {
                $"INSERT INTO airline VALUES ('AL{maxID + 1}', '{name}', '{IATA}', '{ICAO}', {Convert.ToInt32(isActive)}, {rating});"
            };
            dbh.ExecuteNonQuery(query);
            return response;

        }

        public HttpResponseMessage RemoveAirline(string airlineID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            List<string> query = new List<string>()
            {
                $"DELETE FROM airline WHERE id='{airlineID}';"
            };
            dbh.ExecuteNonQuery(query);
            return response;
        }


        /// <summary>
        /// The function adds a new airplane into the airplane database.
        /// </summary>
        /// <param name="airplaneName">The full name of the airplane.</param>
        /// <param name="IATA">The IATA code of the airplane (3 characters).</param>
        /// <param name="ICAO">The IATA code of the airplane (4 characters).</param>
        /// <param name="cruiseSpeed">The cruise speed of the airplane in kts.</param>
        /// <param name="numOfSeats">Number of seats in the airplane.</param>
        public HttpResponseMessage AddAirplane(string airplaneName, string IATA, string ICAO, int cruiseSpeed, int numOfSeats)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            if (IATA.Length != 3 || ICAO.Length != 4)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            List<string> query = new List<string>()
            { 
                $"INSERT INTO airplane VALUES ('{airplaneName}', '{IATA}', '{ICAO}', {cruiseSpeed}, {numOfSeats});"
            };
            dbh.ExecuteNonQuery(query);
            return response;
        }


        /// <summary>
        /// The function removes an airplane from the airplane table.
        /// </summary>
        /// <param name="IATA">The IATA code of the airplane (3 characters).</param>
        public HttpResponseMessage RemoveAirplane(string IATA)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            if (IATA.Length != 3)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            List<string> query = new List<string>()
            {
                $"DELETE FROM airplane WHERE IATA='{IATA}';"
            };
            dbh.ExecuteNonQuery(query);
            return response;
        }


        /// <summary>
        /// The function adds a new airport to the airport table.
        /// </summary>
        /// <param name="name">The full name of the airport.</param>
        /// <param name="city">The city of the airport.</param>
        /// <param name="country">The country of the airport.</param>
        /// <param name="IATA">The airport's IATA code (3 characters).</param>
        /// <param name="ICAO">The airport's ICAO code (4 characters).</param>
        /// <param name="lat">The latitude of the airport location.</param>
        /// <param name="lon">The longitude of the airport location.</param>
        /// <param name="timezone"></param>
        public HttpResponseMessage AddAirport(string name, string city, string country, string IATA, 
            string ICAO, double lat, double lon, double timezone)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            if (IATA.Length != 3 || ICAO.Length != 4 || lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            int maxID = Convert.ToInt32(dbh.ExecuteQuery("SELECT substr(id, 3) FROM airport ORDER BY substr(id, 3) * 1 DESC LIMIT 1;")
                .Item2[0][0]); 
            List<string> query = new List<string>()
            {
                $"INSERT INTO airport VALUES ('AP{maxID + 1}', '{name}', '{city}', '{country}', '{IATA}', '{ICAO}', {lat}, {lon}, {timezone}, 0, 0);"
            };
            dbh.ExecuteNonQuery(query);
            return response;
        }


        /// <summary>
        /// The function removes an airport for the airport table.
        /// </summary>
        /// <param name="id">The airport's id.</param>
        public void RemoveAirport(string id)
        {
            List<string> query = new List<string>()
            {
                $"DELETE FROM airport WHERE id='{id}';"
                
            };
            dbh.ExecuteNonQuery(query);
        }


        /// <summary>
        /// The function adds a new route into the route table.
        /// </summary>
        /// <param name="sourceID">The id of the source airport.</param>
        /// <param name="destinationID">The id of the destination airport.</param>
        /// <param name="airlineID">The airline's ID.</param>
        /// <param name="equipment">The airplanes that can fly that route.</param>
        public void AddRoute(string sourceID, string destinationID, string airlineID, string equipment)
        {
            List<string> query = new List<string>()
            {
                $"CALL AddRoute('{sourceID}', '{destinationID}', '{destinationID}', '{equipment}');"
            };
            dbh.ExecuteNonQuery(query);
        }


        /// <summary>
        /// The function removes the route from the route table.
        /// </summary>
        /// <param name="routeID">The route's id.</param>
        public void RemoveRoute(string routeID)
        {
            List<string> query = new List<string>()
            {
                $"CALL RemoveRoute('{routeID}');"
            };
            dbh.ExecuteNonQuery(query);
        }


        /// <summary>
        /// The function adds a flight into the flight table.
        /// </summary>
        /// <param name="routeID">The flight's route ID.</param>
        /// <param name="departureTimeGMT">The departure time GMT.</param>
        /// <param name="arrivalTimeGMT">The arrival time GMT.</param>
        /// <param name="ticketPrice">Ticket price.</param>
        /// <param name="airplane">Airplane flying the route.</param>
        public HttpResponseMessage AddFlight(string routeID, string departureTimeGMT, string arrivalTimeGMT, int ticketPrice, string airplaneIATA)
        {
            var respone = new HttpResponseMessage();
            respone.StatusCode = HttpStatusCode.OK;
            if (dbh.ExecuteQuery($"SELECT COUNT(id) FROM route WHERE id='{routeID}';").Item2[0][0] == "0" ||
                dbh.ExecuteQuery($"SELECT COUNT(IATA) FROM airplane WHERE IATA='{airplaneIATA}';").Item2[0][0] == "0")
            {
                respone.StatusCode = HttpStatusCode.BadRequest;
                return respone;
            }

            int maxID = Convert.ToInt32(dbh.ExecuteQuery("SELECT substr(id, 2) FROM flight ORDER BY substr(id, 2) * 1 DESC LIMIT 1;")
                .Item2[0][0]);
            var numOfSeats = dbh.ExecuteQuery($"SELECT number_of_seats FROM airplane WHERE IATA='{airplaneIATA}';").Item2[0][0];
            List<string> query = new List<string>()
            {
                // TODO: fix MAXID.
                $"INSERT INTO flight VALUES ('F{maxID + 1}', '{routeID}', '{departureTimeGMT}', '{arrivalTimeGMT}', {numOfSeats}, " +
                $"{ticketPrice}, '{airplaneIATA}');"
        };

            dbh.ExecuteNonQuery(query);
            return respone;
        }


        /// <summary>
        ///  Delete a flight from the flight database.
        /// </summary>
        /// <param name="flightID">The flights ID.</param>
        /// <remarks>When you remove a flight, all the reservations with this flight will also be deleted.</remarks>
        public HttpResponseMessage RemoveFlight(string flightID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            string query = $"SELECT departure_time_GMT FROM flight WHERE id='{flightID}';";

            // If it's an old flight, don't remove it.
            //if (DateTime.Compare(DateTime.Parse(dbh.ExecuteQuery(query).Item2[0][0]), DateTime.Now) < 0)
            //{
            //    response.StatusCode = HttpStatusCode.Forbidden;
            //    return response;
            //}
            query = $"SELECT id, user FROM reservation WHERE flight='{flightID}';";
            var reservations = dbh.ExecuteQuery(query);


            foreach(var reservation in reservations.Item2)
            {
                query = $"CALL FindReservationData('{reservation[reservations.Item1["id"]]}');";
                var reservationDataQuery = dbh.ExecuteQuery(query);
                var reservationData = reservationDataQuery.Item2;
                var reservationsHeader = reservationDataQuery.Item1;

                string cancelSubject = $"Cancellation of your {reservation[reservations.Item1["id"]]} flight reservation";
                string body = $"Dear {reservationData[0][reservationsHeader["full_name"]]},\n" +
                    $"we are sorry to inform you that due to the cancellation of {reservationData[0][reservationsHeader["airline_name"]]} " +
                    $"flight {flightID} (flight number {reservationData[0][reservationsHeader["flight_number"]]}) we had " +
                    $"to cancel your {reservation[reservations.Item1["id"]]} reservation from " +
                    $"{reservationData[0][reservationsHeader["departure_airport"]]}" +
                    $" to {reservationData[0][reservationsHeader["destination_airport"]]} on the" +
                    $" {DateTime.Parse(reservationData[0][reservationsHeader["local_departure_time"]]).ToShortDateString()}.\n" +
                    $"We apologize for the inconvenience.\n" +
                    $"Please visit our website to book a new reservation.";

                SendEmail(reservation[reservations.Item1["user"]], cancelSubject, body);
                CancelReservation(reservation[reservations.Item1["id"]]);
            }
            // Remove the flight.
            List<string> queries = new List<string>()
            {
                $"DELETE FROM flight WHERE id='{flightID}';"
            };
            dbh.ExecuteNonQuery(queries);

            return response;
        }

        /// <summary>
        /// The function sends an email to inform the user that his reservation is canceled.
        /// </summary>
        /// <param name="destinationEmail">The customer email.</param>
        /// <param name="subject">The email's subject.</param>
        /// <param name="body">The email's body.</param>
        private void SendEmail(string destinationEmail, string subject, string body)
        {

            var fromAddress = new MailAddress(this.emailAddress, "Flights booking website");
            var toAddress = new MailAddress(destinationEmail, destinationEmail);
            string fromPassword = this.emailPassword;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                Timeout = 20000
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }


        /// <summary>
        /// The function finds and airport that the 'airportName' is the substring of it.
        /// </summary>
        /// <param name="airportName">The substring of the airport.</param>
        /// <returns>A list of airports names that match the substring.</returns>
        public string FindAirport(string airportName)
        {
            int resultsLimit = 5;
            string query = $"SELECT * FROM autocomplete WHERE name LIKE '%{airportName}%' OR" +
                $" ICAO LIKE '%{airportName}%' OR IATA LIKE '%{airportName}%' LIMIT {resultsLimit};";
            Tuple<Dictionary<string, int>, List<List<string>>> reservations = dbh.ExecuteQuery(query);

            var headers = reservations.Item1;
            var tableValues = reservations.Item2;

            JArray airportsResult = new JArray();

            foreach (var airport in tableValues)
            {
                dynamic jAirport    = new JObject();
                jAirport.name       = airport[headers["name"]];
                jAirport.IATA       = airport[headers["IATA"]];
                jAirport.ICAO       = airport[headers["ICAO"]];
                jAirport.airport_id = airport[headers["airport_id"]];
                airportsResult.Add(jAirport);
            }
            return airportsResult.ToString();
        }


        /// <summary>
        /// The function returns all customer's reservations.
        /// </summary>
        /// <param name="userID">The user's ID.</param>
        /// <returns>A JSON string of all the reservations the costumer have.</returns>
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
                        //************ Calculate total duration of reservation.
                        string metaqQuery = $"CALL FindReservationData('{lastReservationID}');";
                        Tuple<Dictionary<string, int>, List<List<string>>> metadata = dbh.ExecuteQuery(metaqQuery);
                        var metadataHeaders = metadata.Item1;
                        var metadataTableValues = metadata.Item2;
                        reservationObj.duration = metadataTableValues[0][metadataHeaders["total_flight_duration"]];
                        //************
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

                        flight.flight_duration = (DateTime.Parse(tableValues[i][headers["GMT_arrival_time"]]) -
                            DateTime.Parse(tableValues[i][headers["GMT_departure_time"]])).ToString();

                        reservationObj.price += Convert.ToInt32(tableValues[i][headers["ticket_price"]]) *
                            Convert.ToInt32(tableValues[i][headers["number_of_passangers"]]);

                        if (i < tableValues.Count - 1)
                        {
                            nextReservationID = tableValues[i + 1][headers["reservation_id"]];

                            if(lastReservationID == nextReservationID)
                            {
                                flight.connection_duration =
                                    (DateTime.Parse(tableValues[i + 1][headers["GMT_departure_time"]].ToString()) -
                                    DateTime.Parse(tableValues[i][headers["GMT_arrival_time"]].ToString())).ToString();
                            }

                        } 
                        else
                        {
                            //************ Calculate total duration of reservation.
                            string metaqQuery2 = $"CALL FindReservationData('{lastReservationID}');";
                            Tuple<Dictionary<string, int>, List<List<string>>> metadata2 = dbh.ExecuteQuery(metaqQuery2);
                            var metadataHeaders2 = metadata2.Item1;
                            var metadataTableValues2 = metadata2.Item2;
                            reservationObj.duration = metadataTableValues2[0][metadataHeaders2["total_flight_duration"]];
                            //************
                        }
                        reservationFlights.Add(flight);
                        i++;
                    }
                }



                reservationObj.flights = reservationFlights;
                reservationsResults.Add(reservationObj);
            }
            return reservationsResults.ToString();
        }


        /// <summary>
        /// The function adds a new user to the user db.
        /// </summary>
        /// <param name="email">The user's unique email.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="DOB">The user date of birth.</param>
        /// <param name="passportID">The user's passport id (not unique).</param>
        /// <returns>If the user was added or not.</returns>
        public HttpResponseMessage SignUp(string email, string fullName, DateTime DOB, string passportID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            var foo = new EmailAddressAttribute();
            response.StatusCode = HttpStatusCode.OK;
            string query = $"SELECT COUNT(*) FROM user WHERE email='{email}';";
            Tuple<Dictionary<string, int>, List<List<string>>> reservations = dbh.ExecuteQuery(query);

            if (Convert.ToInt32(reservations.Item2[0][0]) > 0 || !foo.IsValid(email))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            query = $"INSERT INTO user VALUES('{email}', '{fullName}', '{DOB.ToString("yyyy-MM-dd")}', '{passportID}');";
            dbh.ExecuteNonQuery(new List<string>() { query });
            return response;
        }


        /// <summary>
        /// The function checks if the email and password are of an admin account.
        /// </summary>
        /// <param name="email">The admin's email.</param>
        /// <param name="password">The admin's password.</param>
        /// <returns></returns>
        public string IsAdmin(string email, string password)
        {
            dynamic result = new JObject();
            string query = $"SELECT * FROM admin WHERE user_email='{email}';";
            Tuple<Dictionary<string, int>, List<List<string>>> reservations = dbh.ExecuteQuery(query);
            var headers = reservations.Item1;
            var tableValues = reservations.Item2;
            if(tableValues.Count == 0)
            {
                result.is_admin = "false";
            }
            else if (tableValues[0].Count > 0 && tableValues[0][headers["password"]] == password)
            {
                result.is_admin = "true";
                return result.ToString();
            }
            result.is_admin = "false";
            return result.ToString();

        }
    }
}
