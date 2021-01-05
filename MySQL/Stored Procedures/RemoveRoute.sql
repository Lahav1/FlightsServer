CREATE DEFINER=`root`@`localhost` PROCEDURE `RemoveRoute`(route_id VARCHAR(45))
proc_label:BEGIN

	IF (SELECT id FROM route WHERE id=route_id IS NULL) THEN
		LEAVE proc_label;
	END IF;
    
    -- Update the number of outbound routes from the source airport.
    UPDATE
		airport
	SET
		number_of_outbound_routes=number_of_outbound_routes - 1
	WHERE
		id=(
			SELECT source_airport FROM route WHERE id=route_id
        );
        
	-- Update the number of inbound routes in the destination airport.
	UPDATE
		airport
	SET
		number_of_inbound_routes=number_of_inbound_routes - 1
	WHERE
		id=(
			SELECT destination_airport FROM route WHERE id=route_id
        );
        
	DELETE FROM route WHERE id=route_id;

END