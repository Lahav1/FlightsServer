CREATE DEFINER=`root`@`localhost` PROCEDURE `CancelReservation`(reservation_id VARCHAR(45))
BEGIN
	DECLARE done INT DEFAULT FALSE;
    DECLARE flight_number VARCHAR(45);
	
	DECLARE flight_id CURSOR FOR SELECT flight FROM reservation WHERE id=reservation_id;
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
	
	OPEN flight_id;
	read_loop: LOOP
        FETCH flight_id INTO flight_number;
		IF done THEN
			LEAVE read_loop;
		END IF;
		
        UPDATE
			flight
        SET
			available_seats=available_seats + (SELECT number_of_passangers FROM reservation WHERE flight=flight_number AND reservation.id=reservation_id)
		WHERE
			flight.id=flight_number;
            
		DELETE FROM reservation WHERE id=reservation_id AND flight=flight_number;
	END LOOP;

END