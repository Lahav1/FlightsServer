CREATE DEFINER=`root`@`localhost` PROCEDURE `FindFlights`(source_airport VARCHAR(10),
	destination_airport VARCHAR(10), departure_date DATE, number_of_tickets INTEGER)
BEGIN

	DROP TABLE IF EXISTS flights_table;
	CREATE TEMPORARY TABLE IF NOT EXISTS flights_table
    (flight1 VARCHAR(10), flight2 VARCHAR(10), flight3 VARCHAR(10), price INTEGER, duration TIME);
    
    CALL FindDirectFlightsLocalTime(source_airport, destination_airport, departure_date, number_of_tickets);
    CALL FindConnectingFlightsAndAirports(source_airport, destination_airport, departure_date, number_of_tickets);
    CALL FindThreeConnectingFlightsAndAirports(source_airport, destination_airport, departure_date, number_of_tickets);
    -- SELECT * FROM flights_table;
    
    DROP TABLE IF EXISTS sorted_flights_table;
	CREATE TEMPORARY TABLE IF NOT EXISTS sorted_flights_table
    (flight1 VARCHAR(10), flight2 VARCHAR(10), flight3 VARCHAR(10), price INTEGER, duration TIME);
    
	INSERT INTO sorted_flights_table (SELECT * FROM flights_table ORDER BY duration, price LIMIT 20);
	INSERT INTO sorted_flights_table (SELECT * FROM flights_table ORDER BY price, duration LIMIT 20);

    SELECT DISTINCT
		flight1.id AS flight1_id, CONCAT(airline1.IATA, SUBSTR(flight1.route, 2)) AS flight1_flight_number,
        DATE_ADD(flight1.departure_time_GMT, INTERVAL leg1_src_airport.timezone HOUR) AS flight1_departure_time_local,
        DATE_ADD(flight1.arrival_time_GMT, INTERVAL leg1_dest_airport.timezone HOUR) AS flight1_arrival_time_local,
        airline1.airline_name AS flight1_airline_name, airline1.rating AS flight1_airline_rating,
		airplane1.airplane_name AS flight1_airplane,
		leg1_src_airport.airport_name AS leg1_src_airport_name, leg1_src_airport.city AS leg1_src_airport_city, leg1_src_airport.country AS leg1_src_airport_country,
        leg1_src_airport.latitude AS leg1_src_airport_lat, leg1_src_airport.longitude AS leg1_src_airport_lon,
		leg1_dest_airport.airport_name AS leg1_dest_airport_name, leg1_dest_airport.city AS leg1_dest_airport_city, leg1_dest_airport.country AS leg1_dest_airport_country,
        leg1_dest_airport.latitude AS leg1_dest_airport_lat, leg1_dest_airport.longitude AS leg1_dest_airport_lon,
        TIMEDIFF(flight1.arrival_time_GMT, flight1.departure_time_GMT) AS flight1_duration,
        TIMEDIFF(flight2.departure_time_GMT, flight1.arrival_time_GMT) AS leg1_connection_duration,
        
		flight2.id AS flight2_id, CONCAT(airline2.IATA, SUBSTR(flight2.route, 2)) AS flight2_flight_number,
        DATE_ADD(flight2.departure_time_GMT, INTERVAL leg2_src_airport.timezone HOUR) AS flight2_departure_time_local,
        DATE_ADD(flight2.arrival_time_GMT, INTERVAL leg2_dest_airport.timezone HOUR) AS flight2_arrival_time_local,
        airline2.airline_name AS flight2_airline_name, airline2.rating AS flight2_airline_rating,
		airplane2.airplane_name AS flight2_airplane,
		leg2_src_airport.airport_name AS leg2_src_airport_name, leg2_src_airport.city AS leg2_src_airport_city, leg2_src_airport.country AS leg2_src_airport_country,
        leg2_src_airport.latitude AS leg2_src_airport_lat, leg2_src_airport.longitude AS leg2_src_airport_lon,
		leg2_dest_airport.airport_name AS leg2_dest_airport_name, leg2_dest_airport.city AS leg2_dest_airport_city, leg2_dest_airport.country AS leg2_dest_airport_country,
        leg2_dest_airport.latitude AS leg2_dest_airport_lat, leg2_dest_airport.longitude AS leg2_dest_airport_lon,
        TIMEDIFF(flight2.arrival_time_GMT, flight2.departure_time_GMT) AS flight2_duration,
        TIMEDIFF(flight3.departure_time_GMT, flight2.arrival_time_GMT) AS leg2_connection_duration,
        
		flight3.id AS flight3_id, CONCAT(airline3.IATA, SUBSTR(flight3.route, 2)) AS flight3_flight_number,
        DATE_ADD(flight3.departure_time_GMT, INTERVAL leg3_src_airport.timezone HOUR) AS flight3_departure_time_local,
        DATE_ADD(flight3.arrival_time_GMT, INTERVAL leg3_dest_airport.timezone HOUR) AS flight3_arrival_time_local,
        airline3.airline_name AS flight3_airline_name, airline3.rating AS flight3_airline_rating,
		airplane3.airplane_name AS flight3_airplane,
		leg3_src_airport.airport_name AS leg3_src_airport_name, leg3_src_airport.city AS leg3_src_airport_city, leg3_src_airport.country AS leg3_src_airport_country,
        leg3_src_airport.latitude AS leg3_src_airport_lat, leg3_src_airport.longitude AS leg3_src_airport_lon,
		leg3_dest_airport.airport_name AS leg3_dest_airport_name, leg3_dest_airport.city AS leg3_dest_airport_city, leg3_dest_airport.country AS leg3_dest_airport_country,
        leg3_dest_airport.latitude AS leg3_dest_airport_lat, leg3_dest_airport.longitude AS leg3_dest_airport_lon,
        TIMEDIFF(flight3.arrival_time_GMT, flight3.departure_time_GMT) AS flight3_duration,
		
        price, duration
	FROM sorted_flights_table AS flights
		LEFT JOIN 
			flight AS flight1 ON flights.flight1=flight1.id
		LEFT JOIN
			flight AS flight2 ON flights.flight2=flight2.id
		LEFT JOIN
			flight AS flight3 ON flights.flight3=flight3.id
		LEFT JOIN
			route AS route1 ON flight1.route=route1.id
		LEFT JOIN
			route AS route2 ON flight2.route=route2.id
		LEFT JOIN
			route AS route3 ON flight3.route=route3.id
		LEFT JOIN
			airline AS airline1 ON route1.airline=airline1.id
		LEFT JOIN
			airline AS airline2 ON route2.airline=airline2.id
		LEFT JOIN
			airline AS airline3 ON route3.airline=airline3.id
		LEFT JOIN
			airplane AS airplane1 ON flight1.airplane=airplane1.IATA
		LEFT JOIN
			airplane AS airplane2 ON flight2.airplane=airplane2.IATA
		LEFT JOIN
			airplane AS airplane3 ON flight3.airplane=airplane3.IATA
		LEFT JOIN
			airport AS leg1_src_airport ON route1.source_airport=leg1_src_airport.id
		LEFT JOIN
			airport AS leg1_dest_airport ON route1.destination_airport=leg1_dest_airport.id
		LEFT JOIN
			airport AS leg2_src_airport ON route2.source_airport=leg2_src_airport.id
		LEFT JOIN
			airport AS leg2_dest_airport ON route2.destination_airport=leg2_dest_airport.id
		LEFT JOIN
			airport AS leg3_src_airport ON route3.source_airport=leg3_src_airport.id
		LEFT JOIN
			airport AS leg3_dest_airport ON route3.destination_airport=leg3_dest_airport.id;
		
    DROP TABLE IF EXISTS flights_table, sorted_flights_table;
END