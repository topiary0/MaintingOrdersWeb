USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'MaintainingOrders')
BEGIN
    ALTER DATABASE MaintainingOrders SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MaintainingOrders;
END
GO

CREATE DATABASE MaintainingOrders;
GO

USE MaintainingOrders;
GO

CREATE TABLE Roles (
    role_id INT PRIMARY KEY IDENTITY(1,1),
    role_name NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    user_id INT PRIMARY KEY IDENTITY(1,1),
    full_name NVARCHAR(100) NOT NULL,
    login NVARCHAR(50) NOT NULL UNIQUE,
    password NVARCHAR(MAX) NOT NULL,
    role_id INT NOT NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (role_id) REFERENCES Roles(role_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);

CREATE TABLE Clients (
    client_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(MAX) NOT NULL,
    address NVARCHAR(250),
    phone NVARCHAR(20),
    contact_person NVARCHAR(100)
);

CREATE TABLE Suppliers (
    suppliers_id INT PRIMARY KEY IDENTITY(1,1),
    supplier_name NVARCHAR(MAX) NOT NULL,
    address NVARCHAR(MAX),
    contact_info NVARCHAR(MAX)
);

CREATE TABLE Delivery_methods (
    method_id INT PRIMARY KEY IDENTITY(1,1),
    method_name NVARCHAR(100) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    estimated_time NVARCHAR(50)
);

CREATE TABLE Order_status (
    status_id INT PRIMARY KEY IDENTITY(1,1),
    status_name NVARCHAR(MAX) NOT NULL
);

CREATE TABLE Shipment_status (
    status_id INT PRIMARY KEY IDENTITY(1,1),
    status_name NVARCHAR(MAX) NOT NULL
);

CREATE TABLE Products (
    product_id INT PRIMARY KEY IDENTITY(1,1),
    article NVARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(MAX) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    description NVARCHAR(MAX),
    sale_price DECIMAL(10,2),
    purchase_price DECIMAL(10,2),
    remains INT DEFAULT 0,
    weight DECIMAL(10,2),
    dimensions NVARCHAR(50),
    suppliers_id INT NOT NULL,
    CONSTRAINT FK_Products_Suppliers 
        FOREIGN KEY (suppliers_id) 
        REFERENCES Suppliers(suppliers_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);

CREATE TABLE Orders (
    order_id INT PRIMARY KEY IDENTITY(1,1),
    order_date DATE NOT NULL DEFAULT GETDATE(),
    status_id INT NOT NULL,
    total_price DECIMAL(10,2) NOT NULL DEFAULT 0,
    delivery_address NVARCHAR(MAX),
    client_id INT NOT NULL,
    user_id INT NOT NULL,
    method_id INT NOT NULL,
    CONSTRAINT FK_Orders_Order_status 
        FOREIGN KEY (status_id) 
        REFERENCES Order_status(status_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Orders_Clients 
        FOREIGN KEY (client_id) 
        REFERENCES Clients(client_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Orders_Users 
        FOREIGN KEY (user_id) 
        REFERENCES Users(user_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Orders_Delivery_methods 
        FOREIGN KEY (method_id) 
        REFERENCES Delivery_methods(method_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);

CREATE TABLE Order_items (
    order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price_at_order DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Order_items PRIMARY KEY (order_id, product_id),
    CONSTRAINT FK_Order_items_Orders 
        FOREIGN KEY (order_id) 
        REFERENCES Orders(order_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Order_items_Products 
        FOREIGN KEY (product_id) 
        REFERENCES Products(product_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);

CREATE TABLE Shipment (
    shipment_id INT PRIMARY KEY IDENTITY(1,1),
    shipment_date DATE NOT NULL DEFAULT GETDATE(),
    status NVARCHAR(20),
    suppliers_id INT NOT NULL,
    user_id INT NOT NULL,
    statussh_id INT NOT NULL,
    CONSTRAINT FK_Shipment_Suppliers 
        FOREIGN KEY (suppliers_id) 
        REFERENCES Suppliers(suppliers_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Shipment_Users 
        FOREIGN KEY (user_id) 
        REFERENCES Users(user_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    CONSTRAINT FK_Shipment_Shipment_status 
        FOREIGN KEY (statussh_id) 
        REFERENCES Shipment_status(status_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);

CREATE TABLE Supply_items (
    shipment_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price_at_shipment DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Supply_items PRIMARY KEY (shipment_id, product_id),
    CONSTRAINT FK_Supply_items_Shipment 
        FOREIGN KEY (shipment_id) 
        REFERENCES Shipment(shipment_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,  
    CONSTRAINT FK_Supply_items_Products 
        FOREIGN KEY (product_id) 
        REFERENCES Products(product_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE       
);

INSERT INTO Roles (role_name) VALUES
(N'Директор'),      -- директор
(N'Менеджер'),    -- менеджер
(N'Сотрудник'),   -- сотрудник 
(N'Бухгалтер'), -- бухгалтер
(N'Логист');-- логист

INSERT INTO Delivery_methods (method_name, price, estimated_time) VALUES
(N'Фармацевтический курьер', 350.00, '1 день'),
(N'Транспортная компания', 200.00, '3-5 дней'),
(N'Самовывоз со склада', 0.00, 'В день заказа');

INSERT INTO Order_status (status_name) VALUES
(N'Новый'), (N'В обработке'), (N'Выполнен'), (N'Отменён');

INSERT INTO Shipment_status (status_name) VALUES
(N'Ожидание'), (N'В пути'), (N'Доставлен'), (N'Возврат');

INSERT INTO Clients (name, address, phone, contact_person) VALUES
(N'Аптека "36,6"', N'г. Москва, ул. Тверская, д.12', N'+7(495)123-45-67', N'Козлова И.М.'),
(N'ГУП "Фармация"', N'г. Санкт-Петербург, пр. Науки, д.15', N'+7(812)555-66-77', N'Смирнов А.А.'),
(N'ООО "Здоровые решения"', N'г. Казань, ул. Павлюхина, д.7', N'+7(843)222-33-44', N'Валеева Л.Р.');

INSERT INTO Suppliers (supplier_name, address, contact_info) VALUES
(N'ЗАО "Катрен"', N'г. Новосибирск, ул. Станционная, д.80', N'тел: 8-800-200-95-95'),
(N'АО "Протек"', N'г. Москва, ул. Профсоюзная, д.65', N'email: info@protek.ru'),
(N'ООО "Пульс"', N'г. Екатеринбург, пер. Базовый, д.5', N'тел: +7(343)379-09-09');

INSERT INTO Users (full_name, login, password, role_id) VALUES
(N'Иванова Анна', 'ivanova', 'pass123', 1), 
(N'Петров Сергей', 'petrov', 'pass456', 2),  
(N'Сидорова Елена', 'sidorova', 'pass789', 3); 

INSERT INTO Products (article, name, price, description, sale_price, purchase_price, remains, weight, dimensions, suppliers_id) VALUES
(N'ART-001', N'Парацетамол таб. 500мг №20', 45.50, N'Обезболивающее, жаропонижающее', 42.00, 30.00, 150, 0.05, N'8x5x3 см', 1),
(N'ART-002', N'Амоксициллин капс. 500мг №16', 120.30, N'Антибиотик широкого спектра', 115.00, 90.00, 80, 0.07, N'9x6x3 см', 2),
(N'ART-003', N'Витамин С шип. таб. №20', 180.00, N'Аскорбиновая кислота', 165.00, 130.00, 200, 0.15, N'10x10x5 см', 1),
(N'ART-004', N'Бинт стерильный 10х15', 35.20, N'Медицинский бинт', 32.00, 22.00, 500, 0.10, N'10x5x5 см', 3),
(N'ART-005', N'Но-шпа таб. 40мг №60', 220.50, N'Спазмолитик', 210.00, 160.00, 60, 0.08, N'8x5x4 см', 2);

INSERT INTO Orders (order_date, status_id, total_price, delivery_address, client_id, user_id, method_id) VALUES
('2025-02-20', 3, 10120.40, N'г. Москва, ул. Тверская, д.12', 1, 1, 1),
('2025-02-22', 2, 5760.00, N'г. Санкт-Петербург, пр. Науки, д.15', 2, 2, 2),
('2025-02-25', 1, 4200.00, N'г. Казань, ул. Павлюхина, д.7', 3, 1, 3);

INSERT INTO Order_items (order_id, product_id, quantity, price_at_order) VALUES
(1, 1, 100, 42.00),
(1, 3, 50, 165.00),
(2, 2, 40, 115.00),
(2, 4, 200, 32.00),
(3, 5, 20, 210.00);

INSERT INTO Shipment (shipment_date, status, suppliers_id, user_id, statussh_id) VALUES
('2025-02-18', 'Delivered', 1, 2, 3),
('2025-02-23', 'In Transit', 2, 1, 2),
('2025-02-26', 'Pending', 3, 3, 1);

INSERT INTO Supply_items (shipment_id, product_id, quantity, price_at_shipment) VALUES
(1, 1, 500, 30.00),
(1, 3, 300, 130.00),
(2, 2, 200, 90.00),
(2, 5, 150, 160.00),
(3, 4, 1000, 22.00);
