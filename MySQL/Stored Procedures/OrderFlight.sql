CREATE DEFINER=`root`@`localhost` PROCEDURE `OrderFlight`(reservation_id VARCHAR(45), email VARCHAR(50), flight_number VARCHAR(45), number_of_tickets INTEGER)
proc_label:BEGIN

	-- If there is no user with this id or not enought tickets leave the procedure.
	IF ((SELECT id FROM user WHERE id=email IS NULL) 
		OR (SELECT available_seats FROM flight WHERE id=flight_number AND available_seats>=number_of_tickets IS null)) THEN
			LEAVE proc_label;
	END IF;
    
    -- Remove number the number of tickets from the flight.
	UPDATE flight
    SET
		available_seats = available_seats - number_of_tickets
	WHERE
		id=flight_number;
	
    -- Add the reservation to the user.
    INSERT INTO reservation (id, user, flight, number_of_passangers)
    VALUES
    (
		reservation_id,
        email,
        flight_number,
        number_of_tickets
        );
END