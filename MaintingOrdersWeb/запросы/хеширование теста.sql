INSERT INTO Users (full_name, login, password, role_id) 
VALUES (N'Тестовый', 'test', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'test123'), 2)), 1);