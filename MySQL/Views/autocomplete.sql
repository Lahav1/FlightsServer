CREATE 
    ALGORITHM = UNDEFINED 
    DEFINER = `root`@`localhost` 
    SQL SECURITY DEFINER
VIEW `autocomplete` AS
    SELECT 
        CONCAT(`airport`.`airport_name`,
                ', ',
                `airport`.`city`,
                ', ',
                `airport`.`country`) AS `name`,
        `airport`.`IATA` AS `IATA`,
        `airport`.`ICAO` AS `ICAO`,
        `airport`.`id` AS `airport_id`
    FROM
        `airport`
    WHERE
        ((`airport`.`number_of_inbound_routes` > 0)
            AND (`airport`.`number_of_outbound_routes` > 0))
    ORDER BY (`airport`.`number_of_inbound_routes` + `airport`.`number_of_outbound_routes`) DESC