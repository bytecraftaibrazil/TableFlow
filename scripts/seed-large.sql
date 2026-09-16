/*
    TableFlow - carga de dados relacionados para SQL Server

    Registros gerados:
      - 10 restaurantes
      - 80 mesas (8 por restaurante)
      - 480 reservas (6 por mesa)

    A carga pode ser executada novamente: os registros identificados por ela
    não são duplicados. Nenhum dado preexistente é apagado ou alterado.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @InsertedRestaurants int = 0;
DECLARE @InsertedTables int = 0;
DECLARE @InsertedReservations int = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 1. Restaurants
    ---------------------------------------------------------------------------
    DECLARE @RestaurantSeed table
    (
        Code varchar(3) NOT NULL PRIMARY KEY,
        Name nvarchar(120) NOT NULL,
        CuisineType nvarchar(80) NOT NULL,
        City nvarchar(80) NOT NULL,
        IsActive bit NOT NULL
    );

    INSERT INTO @RestaurantSeed (Code, Name, CuisineType, City, IsActive)
    VALUES
        ('R01', N'TF Demo - Sabor Paulista',      N'Brasileira',     N'São Paulo',      1),
        ('R02', N'TF Demo - Cantina Toscana',     N'Italiana',       N'Campinas',       1),
        ('R03', N'TF Demo - Maré Alta',           N'Frutos do Mar',  N'Rio de Janeiro', 1),
        ('R04', N'TF Demo - Jardim Oriental',     N'Japonesa',       N'Curitiba',        1),
        ('R05', N'TF Demo - Fuego Mexicano',      N'Mexicana',       N'Belo Horizonte', 1),
        ('R06', N'TF Demo - Bistrô do Porto',     N'Francesa',       N'Porto Alegre',    1),
        ('R07', N'TF Demo - Raízes do Nordeste',  N'Nordestina',     N'Recife',          1),
        ('R08', N'TF Demo - Pampa Grill',         N'Churrascaria',   N'Florianópolis',   1),
        ('R09', N'TF Demo - Veg & Vida',          N'Vegetariana',    N'Brasília',        1),
        ('R10', N'TF Demo - Sabores da Bahia',    N'Baiana',         N'Salvador',        0);

    INSERT INTO dbo.Restaurants (Name, CuisineType, City, IsActive)
    SELECT seed.Name, seed.CuisineType, seed.City, seed.IsActive
    FROM @RestaurantSeed AS seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Restaurants AS restaurant
        WHERE restaurant.Name = seed.Name
    );

    SET @InsertedRestaurants = @@ROWCOUNT;

    DECLARE @RestaurantIds table
    (
        Code varchar(3) NOT NULL PRIMARY KEY,
        RestaurantId int NOT NULL
    );

    INSERT INTO @RestaurantIds (Code, RestaurantId)
    SELECT seed.Code, MIN(restaurant.Id)
    FROM @RestaurantSeed AS seed
    INNER JOIN dbo.Restaurants AS restaurant
        ON restaurant.Name = seed.Name
    GROUP BY seed.Code;

    ---------------------------------------------------------------------------
    -- 2. Tables
    -- O índice único (RestaurantId, Number) é respeitado.
    ---------------------------------------------------------------------------
    DECLARE @TableSeed table
    (
        Number int NOT NULL PRIMARY KEY,
        Capacity int NOT NULL,
        IsActive bit NOT NULL
    );

    INSERT INTO @TableSeed (Number, Capacity, IsActive)
    VALUES
        (1,  2, 1),
        (2,  2, 1),
        (3,  4, 1),
        (4,  4, 1),
        (5,  6, 1),
        (6,  6, 1),
        (7,  8, 1),
        (8, 10, 1);

    INSERT INTO dbo.Tables (RestaurantId, Number, Capacity, IsActive)
    SELECT restaurant.RestaurantId, seed.Number, seed.Capacity, seed.IsActive
    FROM @RestaurantIds AS restaurant
    CROSS JOIN @TableSeed AS seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Tables AS restaurantTable
        WHERE restaurantTable.RestaurantId = restaurant.RestaurantId
          AND restaurantTable.Number = seed.Number
    );

    SET @InsertedTables = @@ROWCOUNT;

    DECLARE @TableIds table
    (
        Code varchar(3) NOT NULL,
        RestaurantId int NOT NULL,
        TableId int NOT NULL,
        TableNumber int NOT NULL,
        Capacity int NOT NULL,
        PRIMARY KEY (Code, TableNumber)
    );

    INSERT INTO @TableIds
        (Code, RestaurantId, TableId, TableNumber, Capacity)
    SELECT
        restaurant.Code,
        restaurant.RestaurantId,
        restaurantTable.Id,
        restaurantTable.Number,
        restaurantTable.Capacity
    FROM @RestaurantIds AS restaurant
    INNER JOIN dbo.Tables AS restaurantTable
        ON restaurantTable.RestaurantId = restaurant.RestaurantId
    INNER JOIN @TableSeed AS seed
        ON seed.Number = restaurantTable.Number;

    ---------------------------------------------------------------------------
    -- 3. Reservations
    -- Cria datas entre 30 dias atrás e 89 dias à frente.
    -- PartySize nunca ultrapassa a capacidade da mesa.
    ---------------------------------------------------------------------------
    DECLARE @ReservationSequence table
    (
        SequenceNumber int NOT NULL PRIMARY KEY
    );

    INSERT INTO @ReservationSequence (SequenceNumber)
    VALUES (1), (2), (3), (4), (5), (6);

    ;WITH ReservationData AS
    (
        SELECT
            restaurantTable.RestaurantId,
            restaurantTable.TableId,
            CONCAT(
                N'Carga TF ',
                restaurantTable.Code,
                N'-M',
                RIGHT('00' + CONVERT(varchar(2), restaurantTable.TableNumber), 2),
                N'-R',
                RIGHT('00' + CONVERT(varchar(2), sequence.SequenceNumber), 2)
            ) AS CustomerName,
            DATEADD
            (
                MINUTE,
                660 +
                    ((restaurantTable.TableNumber * 37
                      + sequence.SequenceNumber * 53) % 600),
                DATEADD
                (
                    DAY,
                    ((CONVERT(int, RIGHT(restaurantTable.Code, 2)) * 11
                      + restaurantTable.TableNumber * 5
                      + sequence.SequenceNumber * 7) % 120) - 30,
                    CONVERT(datetime2, CONVERT(date, GETDATE()))
                )
            ) AS ReservationDate,
            1 +
                ((restaurantTable.TableNumber
                  + sequence.SequenceNumber) % restaurantTable.Capacity) AS PartySize,
            CASE
                WHEN (CONVERT(int, RIGHT(restaurantTable.Code, 2))
                      + restaurantTable.TableNumber
                      + sequence.SequenceNumber) % 3 = 0 THEN 'Pending'
                WHEN (CONVERT(int, RIGHT(restaurantTable.Code, 2))
                      + restaurantTable.TableNumber
                      + sequence.SequenceNumber) % 3 = 1 THEN 'Confirmed'
                ELSE 'Cancelled'
            END AS Status
        FROM @TableIds AS restaurantTable
        CROSS JOIN @ReservationSequence AS sequence
    )
    INSERT INTO dbo.Reservations
        (RestaurantId, TableId, CustomerName, ReservationDate, PartySize, Status)
    SELECT
        data.RestaurantId,
        data.TableId,
        data.CustomerName,
        data.ReservationDate,
        data.PartySize,
        data.Status
    FROM ReservationData AS data
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Reservations AS reservation
        WHERE reservation.RestaurantId = data.RestaurantId
          AND reservation.TableId = data.TableId
          AND reservation.CustomerName = data.CustomerName
    );

    SET @InsertedReservations = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @InsertedRestaurants AS RestaurantsInseridos,
        @InsertedTables AS MesasInseridas,
        @InsertedReservations AS ReservasInseridas;

    SELECT
        (SELECT COUNT(*) FROM dbo.Restaurants) AS TotalRestaurants,
        (SELECT COUNT(*) FROM dbo.Tables) AS TotalTables,
        (SELECT COUNT(*) FROM dbo.Reservations) AS TotalReservations;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
