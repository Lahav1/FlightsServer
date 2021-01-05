CREATE DEFINER=`root`@`localhost` PROCEDURE `FindThreeConnectingFlightsAndAirports`(source_airport_ICAO VARCHAR(10), 
	destination_airport_ICAO VARCHAR(10), departure_date DATE, number_of_tickets INTEGER)
BEGIN
	DECLARE done INT DEFAULT FALSE;
    DECLARE connecting_airport1 varchar(10) DEFAULT "";
	DECLARE connecting_airport2 varchar(10) DEFAULT "";
	DECLARE distance DOUBLE DEFAULT 0;

    -- Select only the 10 shortest routes.
	DECLARE connecting_airports CURSOR FOR SELECT first_stop, second_stop,
		GET_DISTANCE(@src_lat, @src_lon, airport1.latitude, airport1.longitude) + 
		GET_DISTANCE(airport1.latitude, airport1.longitude, airport2.latitude, airport2.longitude) + 
        GET_DISTANCE(airport2.latitude, airport2.longitude, @dest_lat, @dest_lon) AS total_distance
        FROM (SELECT DISTINCT source_airport AS first_stop, destination_airport AS second_stop FROM 
 		route WHERE source_airport IN 
 		(SELECT DISTINCT destination_airport FROM route WHERE source_airport=@src_id AND destination_airport<>@dest_id)
 		AND destination_airport IN (SELECT DISTINCT source_airport FROM route 
 		WHERE destination_airport=@dest_id AND source_airport<>@src_id)) AS connections INNER JOIN airport AS airport1 ON 
        first_stop=airport1.id INNER JOIN airport AS airport2 ON second_stop=airport2.id
        ORDER BY total_distance LIMIT 5;
        
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

	SELECT id, timezone, latitude, longitude INTO @src_id, @src_timezone, @src_lat, @src_lon FROM airport WHERE ICAO=source_airport_ICAO;
	SELECT id, latitude, longitude INTO @dest_id, @dest_lat, @dest_lon FROM airport WHERE ICAO=destination_airport_ICAO;

	-- Check the start time and end time of the flights in GMT time.
	SET @departure_time_local:=DATE_SUB(departure_date, INTERVAL @src_timezone HOUR);    
	SET @departure_time_local_plus_24:=DATE_ADD(@departure_time_local, INTERVAL 1 DAY);  
    
    -- Iterate over all airports.
    OPEN connecting_airports;
	read_loop: LOOP
        FETCH connecting_airports INTO connecting_airport1, connecting_airport2, distance;
		IF done THEN
			LEAVE read_loop;
		END IF;
                
		-- Create a temporary table of all departing flights.
		CREATE TEMPORARY TABLE IF NOT EXISTS departing_flights_from_source_to_stop1
		SELECT * FROM flight
		LIMIT 0;
	
		-- Get all the flights departing from the source airport to stop1 the route in ids.
		INSERT INTO departing_flights_from_source_to_stop1 SELECT * 
			FROM flight WHERE route IN (SELECT id FROM route WHERE source_airport=@src_id AND destination_airport=connecting_airport1) 
			AND departure_time_GMT BETWEEN @departure_time_local AND @departure_time_local_plus_24 AND available_seats >= number_of_tickets;
						
		-- Get first and last landing in first connecting airport from source airport.
		SELECT MIN(arrival_time_GMT), MAX(arrival_time_GMT) INTO @first_landing_in_connecting_airport1, @last_landing_in_connecting_airport1
			FROM departing_flights_from_source_to_stop1;

		SET @last_departing_flight_from_connecting_airport1:=DATE_ADD(@last_landing_in_connecting_airport1, INTERVAL 1 DAY);

		-- Create temporary table for departing flights from connecting airport 1 to connectiong airport 2.
		CREATE TEMPORARY TABLE IF NOT EXISTS departing_flights_from_stop1_to_stop2
		SELECT * FROM flight
		LIMIT 0;
		
		INSERT INTO departing_flights_from_stop1_to_stop2 SELECT * 
			FROM flight WHERE route IN (SELECT id FROM route WHERE source_airport=connecting_airport1 AND destination_airport=connecting_airport2) 
			AND departure_time_GMT BETWEEN @first_landing_in_connecting_airport1 AND @last_departing_flight_from_connecting_airport1 AND available_seats >= number_of_tickets;
            
		-- Get first and last landing in second connecting airport from source airport.
		SELECT MIN(arrival_time_GMT), MAX(arrival_time_GMT) INTO @first_landing_in_connecting_airport2, @last_landing_in_connecting_airport2
		FROM departing_flights_from_stop1_to_stop2;

		SET @last_departing_flight_from_connecting_airport2:=DATE_ADD(@last_landing_in_connecting_airport2, INTERVAL 1 DAY);

		-- Create temporary table for departing flights from connecting airport 1 to connectiong airport 2.
		CREATE TEMPORARY TABLE IF NOT EXISTS departing_flights_from_stop2_to_destination
		SELECT * FROM flight
		LIMIT 0;
        
		INSERT INTO departing_flights_from_stop2_to_destination SELECT * 
			FROM flight WHERE route IN (SELECT id FROM route WHERE source_airport=connecting_airport2 AND destination_airport=@dest_id) 
			AND departure_time_GMT BETWEEN @first_landing_in_connecting_airport2 AND @last_departing_flight_from_connecting_airport2 AND available_seats >= number_of_tickets;
        
		INSERT INTO flights_table SELECT /*+ MAX_EXECUTION_TIME(10) */ leg1.id AS flight1, leg2.id AS flight2, leg3.id AS flight3, leg1.ticket_price + leg2.ticket_price + leg3.ticket_price AS price, TIMEDIFF(leg3.arrival_time_GMT, leg1.departure_time_GMT) AS duration 
			FROM departing_flights_from_source_to_stop1 AS leg1 INNER JOIN 
			departing_flights_from_stop1_to_stop2 AS leg2, departing_flights_from_stop2_to_destination AS leg3
			WHERE DATE_SUB(leg2.departure_time_GMT, INTERVAL 20 MINUTE) > leg1.arrival_time_GMT AND 
			DATE_SUB(leg2.departure_time_GMT, INTERVAL 24 HOUR) < leg1.arrival_time_GMT
			AND DATE_SUB(leg3.departure_time_GMT, INTERVAL 20 MINUTE) > leg2.arrival_time_GMT
			AND DATE_SUB(leg3.departure_time_GMT, INTERVAL 24 HOUR) < leg2.arrival_time_GMT;
            
		DROP TABLE departing_flights_from_source_to_stop1, departing_flights_from_stop1_to_stop2, departing_flights_from_stop2_to_destination;
	END LOOP;
END