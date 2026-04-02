USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'MaintainingOrders')
BEGIN
    -- Завершаем все активные соединения с базой данных
    ALTER DATABASE MaintainingOrders SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    
    -- Удаляем базу данных
    DROP DATABASE MaintainingOrders;
    
    PRINT 'База данных MaintainingOrders успешно удалена.';
END
ELSE
BEGIN
    PRINT 'База данных MaintainingOrders не существует.';
END
GO