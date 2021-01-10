CREATE DEFINER=`root`@`localhost` PROCEDURE `AddRoute`(src_id VARCHAR(45),
dest_id VARCHAR(45), airline_id VARCHAR(45), equipment VARCHAR(45))
proc_label:BEGIN
	-- Check that the route source airport and destination airport exist, else exit the procedure.
	IF (SELECT id FROM airport WHERE id=src_id IS NULL) OR
		(SELECT id FROM airport WHERE id=dest_id IS NULL)
        OR (SELECT id FROM airline WHERE id=airline_id IS NULL) THEN
			LEAVE proc_label;
	END IF;
    
    -- Get the new id.
    SELECT substr(id, 2) INTO @max_id FROM route ORDER BY substr(id, 2) * 1 DESC LIMIT 1;
    INSERT INTO route VALUES (CONCAT("R", @max_id + 1), airline_id, src_id, dest_id, equipment);
    
    UPDATE 
		airport
	SET
		number_of_outbound_routes=number_of_outbound_routes + 1
	WHERE
		id=src_id;
        
	UPDATE 
		airport
	SET
		number_of_inbound_routes=number_of_inbound_routes + 1
	WHERE
		id=dest_id;
		
END
