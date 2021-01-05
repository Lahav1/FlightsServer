CREATE DEFINER=`root`@`localhost` FUNCTION `GET_DISTANCE`(lat1 DOUBLE, lon1 DOUBLE, lat2 DOUBLE, lon2 DOUBLE) RETURNS double
    DETERMINISTIC
BEGIN
-- DECLARE result DOUBLE DEFAULT 0;
	SET @result:=(6371 * acos( 
                cos(radians(lat2)) 
              * cos(radians(lat1)) 
              * cos(radians(lon1) - radians(lon2) ) 
              + sin(radians(lat2)) 
              * sin(radians(lat1))
                ));

RETURN @result;
END