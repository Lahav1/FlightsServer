CREATE DEFINER=`root`@`localhost` PROCEDURE `FindConnectingFlightsAndAirports`(source_airport_ICAO VARCHAR(10), 
	destination_airport_ICAO VARCHAR(10), departure_date DATE, number_of_tickets INTEGER)
BEGIN
	DECLARE done INT DEFAULT FALSE;
    DECLARE connecting_airport_id varchar(10) DEFAULT ""; 
    
	DECLARE connecting_airports CURSOR FOR SELECT id FROM airport WHERE id IN 
		(SELECT leg1.destination_airport FROM route AS leg1 INNER JOIN route AS leg2 
		WHERE leg1.source_airport=@src_id AND leg2.destination_airport=@dest_id 
		AND leg1.destination_airport=leg2.source_airport AND leg1.destination_airport<>@dest_id);
    
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

	SELECT id, timezone INTO @src_id, @src_timezone FROM airport WHERE ICAO=source_airport_ICAO;
	SELECT id INTO @dest_id FROM airport WHERE ICAO=destination_airport_ICAO;

	-- Check the start time and end time of the flights in GMT time.
	SET @departure_time_local:=DATE_SUB(departure_date, INTERVAL @src_timezone HOUR);    
	SET @departure_time_local_plus_24:=DATE_ADD(@departure_time_local, INTERVAL 1 DAY);  
	
        
    -- Iterate over all airports.
    OPEN connecting_airports;
	read_loop: LOOP
        FETCH connecting_airports INTO connecting_airport_id;
		IF done THEN
			LEAVE read_loop;
		END IF;
        
		-- Create a temporary table of all departing flights.
		CREATE TEMPORARY TABLE IF NOT EXISTS  departing_flights_from_source
		SELECT * FROM flight
		LIMIT 0;

		-- Get all the flights departing from the source airport to the destination airport connecting at connecting_airport_id.
		INSERT INTO departing_flights_from_source SELECT * 
			FROM flight WHERE route IN (SELECT id FROM route WHERE source_airport=@src_id AND destination_airport=connecting_airport_id) 
			AND departure_time_GMT BETWEEN @departure_time_local AND @departure_time_local_plus_24 AND available_seats >= number_of_tickets;
            
		-- Get first and last landing in connecting airport from source airport.
		SELECT MIN(arrival_time_GMT), MAX(arrival_time_GMT) INTO @first_landing_in_connecting_airport, @last_landing_in_connecting_airport
			FROM departing_flights_from_source;

		SET @last_departing_flight_from_connecting_airport:=DATE_ADD(@last_landing_in_connecting_airport, INTERVAL 1 DAY);

		-- Create temporary table for departing flights from connecting airport.
		CREATE TEMPORARY TABLE IF NOT EXISTS departing_flights_from_connecting_airport
		SELECT * FROM flight
		LIMIT 0;
		
		INSERT INTO departing_flights_from_connecting_airport SELECT * 
			FROM flight WHERE route IN (SELECT id FROM route WHERE source_airport=connecting_airport_id AND destination_airport=@dest_id) 
			AND departure_time_GMT BETWEEN @first_landing_in_connecting_airport AND @last_departing_flight_from_connecting_airport AND available_seats >= number_of_tickets;
            
		 INSERT INTO flights_table SELECT /*+ MAX_EXECUTION_TIME(1) */ leg1.id AS flight1, leg2.id AS flight2, NULL AS flight3, leg1.ticket_price + leg2.ticket_price AS price, TIMEDIFF(leg2.arrival_time_GMT, leg1.departure_time_GMT) AS duration FROM departing_flights_from_source AS leg1 INNER JOIN departing_flights_from_connecting_airport AS leg2
			 WHERE DATE_SUB(leg2.departure_time_GMT, INTERVAL 20 MINUTE) > leg1.arrival_time_GMT AND 
			 DATE_SUB(leg2.departure_time_GMT, INTERVAL 24 HOUR) < leg1.arrival_time_GMT;

		DROP TABLE departing_flights_from_source, departing_flights_from_connecting_airport;
	END LOOP;
END