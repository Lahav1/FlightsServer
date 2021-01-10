CREATE DEFINER=`root`@`localhost` PROCEDURE `FindReservationData`(reservation_id VARCHAR(45))
BEGIN
	SELECT full_name AS full_name,
    MIN(DATE_ADD(flight.departure_time_GMT, INTERVAL source_airport.timezone HOUR)) AS local_departure_time,
	CONCAT(source_airport.city, ", ", source_airport.country) AS departure_airport,
	CONCAT(destination_airport.city, ", ", destination_airport.country) AS destination_airport,
    CONCAT(airline.IATA, SUBSTR(route.id, 2)) AS flight_number,
    airline.airline_name AS airline_name
	FROM reservation LEFT JOIN flight ON(reservation.flight=flight.id)
	LEFT JOIN route ON(flight.route=route.id)
    LEFT JOIN airline ON(route.airline=airline.id)
	LEFT JOIN airport AS source_airport ON(route.source_airport=source_airport.id)
	LEFT JOIN airport AS destination_airport ON(route.destination_airport=destination_airport.id)
    LEFT JOIN user ON(user.email=reservation.user)
	WHERE reservation.id=reservation_id;
END
