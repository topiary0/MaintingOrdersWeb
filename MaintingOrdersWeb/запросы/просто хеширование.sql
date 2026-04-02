UPDATE Users SET password = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'pass123'), 2)) WHERE login = 'ivanova';
UPDATE Users SET password = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'pass456'), 2)) WHERE login = 'petrov';
UPDATE Users SET password = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'pass789'), 2)) WHERE login = 'sidorova';