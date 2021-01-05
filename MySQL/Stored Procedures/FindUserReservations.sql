CREATE DEFINER=`root`@`localhost` PROCEDURE `FindUserReservations`(user_id VARCHAR(45))
BEGIN
	SELECT 
		reservations.id AS reservation_id, reservations.flight AS flight_id,
		reservations.number_of_passangers AS number_of_passangers,
		flight_airline.airline_name AS airline,
        CONCAT(flight_airline.IATA, SUBSTR(flights.route, 2)) AS flight_number,
        flight_airplane.airplane_name AS airplane,
		DATE_ADD(flights.departure_time_GMT, INTERVAL source_airport.timezone HOUR) AS local_departure_time,
        DATE_ADD(flights.arrival_time_GMT, INTERVAL destination_airport.timezone HOUR) AS local_arrival_time,
        
		flights.departure_time_GMT AS GMT_departure_time,
        flights.arrival_time_GMT AS GMT_arrival_time,

		source_airport.airport_name AS source_airport_name, source_airport.city AS source_airport_city,
        source_airport.country AS source_airport_country, source_airport.latitude AS source_airport_latitude,
        source_airport.longitude AS source_airport_longitude,
        
		destination_airport.airport_name AS destination_airport_name, destination_airport.city AS destination_airport_city,
        destination_airport.country AS destination_airport_country, destination_airport.latitude AS destination_airport_latitude,
        destination_airport.longitude AS destination_airport_longitude,
        
        flights.ticket_price AS ticket_price
        
        FROM reservation AS reservations
		LEFT JOIN 
			flight AS flights ON (reservations.flight=flights.id)
        LEFT JOIN 
			route AS routes ON (routes.id=flights.route)
		LEFT JOIN 
			airport AS source_airport ON (source_airport.id=routes.source_airport)
		LEFT JOIN 
			airport AS destination_airport ON (destination_airport.id=routes.destination_airport)
		LEFT JOIN
			airline AS flight_airline ON (routes.airline=flight_airline.id)
		LEFT JOIN
			airplane AS flight_airplane ON (flights.airplane=flight_airplane.IATA)
        WHERE 
			reservations.user=user_id
        ORDER BY
			reservation_id, flights.departure_time_GMT;

END