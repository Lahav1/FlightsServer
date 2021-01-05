CREATE DEFINER=`root`@`localhost` PROCEDURE `FindDirectFlightsLocalTime`(src_airport VARCHAR(10), dest_airport VARCHAR(10), 
	departure_time DATE, number_of_tickets INT)
BEGIN
	-- Get source airport and destination airport id and source airport timezone.
	SELECT id, timezone INTO @src_id, @src_timezone FROM airport WHERE ICAO=src_airport;
	SELECT id INTO @dest_id FROM airport WHERE ICAO=dest_airport;

	-- Check the start time and end time of the flights in GMT time.
	SET @departure_time_local:=DATE_SUB(departure_time, INTERVAL @src_timezone HOUR);    
	SET @departure_time_local_plus_24:=DATE_ADD(@departure_time_local, INTERVAL 1 DAY);    
    
    -- Insert into the flights_table all the direct flights found.
	 INSERT INTO flights_table SELECT /*+ MAX_EXECUTION_TIME(30000) */ id AS flight1, NULL AS flight2, NULL AS flight3, ticket_price AS price, TIMEDIFF(arrival_time_GMT, departure_time_GMT) AS duration FROM flight WHERE route IN 
		(SELECT id FROM route WHERE source_airport=@src_id AND destination_airport=@dest_id) 
		AND departure_time_GMT
		BETWEEN @departure_time_local AND @departure_time_local_plus_24 AND available_seats >= number_of_tickets;
END