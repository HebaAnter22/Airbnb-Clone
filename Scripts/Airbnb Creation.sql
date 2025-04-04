CREATE DATABASE Airbnb_clone;
GO

USE Airbnb_clone;
GO

-- Create Tables
CREATE TABLE [users] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [email] VARCHAR(255) UNIQUE NOT NULL,
  [password_hash] VARCHAR(255) NOT NULL,
  [first_name] VARCHAR(100),
  [last_name] VARCHAR(100),
  [date_of_birth] DATE,
  [gender] VARCHAR(20),
  [profile_picture_url] VARCHAR(500),
  [phone_number] VARCHAR(20),
  [account_status] VARCHAR(20) DEFAULT 'active',
  [email_verified] BIT DEFAULT (0),
  [phone_verified] BIT DEFAULT (0),
  [created_at] DATETIME DEFAULT GETDATE(),
  [updated_at] DATETIME DEFAULT GETDATE(),
  [last_login] DATETIME,
  [role] NVARCHAR(255) DEFAULT 'guest'
);
GO

CREATE TABLE [host] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [user_id] INT,
  [start_date]  DATETIME DEFAULT GETDATE(),
  [my_work] VARCHAR(500),
  [pets] VARCHAR(500),
  [education] VARCHAR(500),
  [funfact] VARCHAR(500),
  [languages] VARCHAR(500),
  [is_verified] BIT DEFAULT (0)
);
GO

CREATE TABLE [cancellation_policies] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [name] VARCHAR(50),
  [description] TEXT
);
GO

CREATE TABLE [properties] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [host_id] INT,
  [title] VARCHAR(255),
  [description] TEXT,
  [property_type] VARCHAR(50),
  [address] VARCHAR(255),
  [city] VARCHAR(100),
  [state] VARCHAR(100),
  [postal_code] VARCHAR(20),
  [latitude] DECIMAL(9,6),
  [longitude] DECIMAL(9,6),
  [price_per_night] DECIMAL(10,2),
  [cleaning_fee] DECIMAL(10,2),
  [service_fee] DECIMAL(10,2),
  [min_nights] INT DEFAULT (1),
  [max_nights] INT,
  [bedrooms] INT,
  [bathrooms] INT,
  [max_guests] INT,
  [check_in_time] TIME,
  [check_out_time] TIME,
  [instant_book] BIT DEFAULT (0),
  [status] VARCHAR(20) DEFAULT 'pending',
  [created_at] DATETIME DEFAULT GETDATE(),
  [updated_at] DATETIME DEFAULT GETDATE(),
  [cancellation_policy_id] INT
);
GO

CREATE TABLE [property_images] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [property_id] INT,
  [image_url] VARCHAR(500),
  [is_primary] BIT DEFAULT (0),
  [category] NVARCHAR(255),
  [created_at] DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE [favourite] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [user_id] INT,
  [property_id] INT
);
GO

CREATE TABLE [amenities] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [name] VARCHAR(100),
  [category] VARCHAR(50),
  [icon_url] VARCHAR(500)
);
GO

CREATE TABLE [property_amenities] (
  [property_id] INT,
  [amenity_id] INT,
  [created_at] DATETIME DEFAULT GETDATE(),
  PRIMARY KEY ([property_id], [amenity_id])
);
GO

CREATE TABLE [bookings] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [property_id] INT,
  [guest_id] INT,
  [start_date] DATE NOT NULL,
  [end_date] DATE NOT NULL,
  [status] VARCHAR(20) NOT NULL
);
GO

CREATE TABLE [booking_payments] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [booking_id] INT,
  [amount] DECIMAL(10,2),
  [payment_method_type] VARCHAR(50) NOT NULL,
  [status] VARCHAR(20),
  [transaction_id] VARCHAR(255),
  [created_at] DATETIME DEFAULT GETDATE(),
  [updated_at] DATETIME DEFAULT GETDATE(),
  [payout_method_type] VARCHAR(50) NOT NULL,
  [paid_out] BIT
);
GO

CREATE TABLE [reviews] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [booking_id] INT,
  [reviewer_id] INT,
  [rating] INT NOT NULL CHECK ([rating] BETWEEN 1 AND 5), -- Added constraint
  [comment] TEXT,
  [created_at] DATETIME DEFAULT GETDATE(),
  [updated_at] DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE [host_verifications] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [user_id] INT,
  [type] VARCHAR(50),
  [status] VARCHAR(20) DEFAULT 'pending',
  [document_url] VARCHAR(500),
  [submitted_at] DATETIME DEFAULT GETDATE(),
  [verified_at] DATETIME
);
GO

CREATE TABLE [promotions] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [code] VARCHAR(50) UNIQUE,
  [discount_type] VARCHAR(20),
  [amount] DECIMAL(10,2),
  [start_date] DATE,
  [end_date] DATE,
  [max_uses] INT,
  [used_count] INT DEFAULT (0),
  [created_at] DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE [user_used_promotions] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [promotions_id] INT,
  [booking_id] INT,
  [user_id] INT
);
GO

CREATE TABLE [conversations] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [created_at] DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE [messages] (
  [id] INT PRIMARY KEY IDENTITY(1, 1),
  [conversation_id] INT,
  [sender_id] INT,
  [receiver_id] INT,
  [content] TEXT,
  [sent_at] DATETIME DEFAULT GETDATE(),
  [read_at] DATETIME
);
GO

use Airbnb_clone

CREATE VIEW vw_property_availability AS
SELECT 
    p.id AS property_id,
    d.date AS available_date,
    CASE 
        WHEN b.id IS NULL AND p.status = 'active' THEN 1
        ELSE 0
    END AS is_available
FROM 
    properties p
CROSS JOIN 
    (SELECT DATEADD(day, n, CAST(GETDATE() AS DATE)) AS date
     FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
           FROM master..spt_values) numbers
     WHERE n < 10) d 
LEFT JOIN 
    bookings b
    ON p.id = b.property_id
    AND d.date BETWEEN b.start_date AND b.end_date
    AND b.status IN ('confirmed', 'pending');


CREATE VIEW vw_property_details AS
SELECT 
    p.id AS property_id,
    p.title,
    p.description,
    p.city,
    p.state,
    p.price_per_night,
    p.cleaning_fee,
    p.service_fee,
    p.property_type,
    p.status,
    h.id AS host_id,
    CONCAT(u.first_name, ' ', u.last_name) AS host_name, 
    u.profile_picture_url AS host_picture,
    COALESCE(AVG(r.rating), 0) AS average_rating, 
    COUNT(r.id) AS review_count
FROM 
    properties p
INNER JOIN 
    host h ON p.host_id = h.id
INNER JOIN 
    users u ON h.user_id = u.id
LEFT JOIN 
    bookings b ON p.id = b.property_id
LEFT JOIN 
    reviews r ON b.id = r.booking_id
GROUP BY 
    p.id, p.title, p.description, p.city, p.state, p.price_per_night, 
    p.cleaning_fee, p.service_fee, p.property_type, p.status, 
    h.id, u.first_name, u.last_name, u.profile_picture_url;


CREATE VIEW vw_user_booking_history AS
SELECT 
    b.id AS booking_id,
    b.guest_id AS user_id,
    u.first_name + ' ' + u.last_name AS guest_name,
    p.title AS property_title,
    b.start_date,
    b.end_date,
    b.status AS booking_status,
    bp.amount,
    bp.payment_method_type,
    bp.status AS payment_status
FROM 
    bookings b
INNER JOIN 
    users u ON b.guest_id = u.id
INNER JOIN 
    properties p ON b.property_id = p.id
LEFT JOIN 
    booking_payments bp ON b.id = bp.booking_id;


	SELECT booking_id, property_title, start_date, end_date, payment_status
FROM vw_user_booking_history
WHERE user_id = 1;



CREATE VIEW vw_host_performance AS
SELECT 
    h.id AS host_id,
    u.first_name + ' ' + u.last_name AS host_name,
    COUNT(DISTINCT p.id) AS total_properties,
    COUNT(DISTINCT b.id) AS total_bookings,
    AVG(r.rating) AS average_rating,
    COUNT(r.id) AS total_reviews
FROM 
    host h
INNER JOIN 
    users u ON h.user_id = u.id
LEFT JOIN 
    properties p ON h.id = p.host_id
LEFT JOIN 
    bookings b ON p.id = b.property_id AND b.status = 'confirmed'
LEFT JOIN 
    reviews r ON b.id = r.booking_id
GROUP BY 
    h.id, u.first_name, u.last_name;


CREATE VIEW vw_active_promotions AS
SELECT 
    p.id AS promotion_id,
    p.code,
    p.discount_type,
    p.amount,
    p.start_date,
    p.end_date,
    p.max_uses,
    p.used_count,
    (p.max_uses - p.used_count) AS remaining_uses
FROM 
    promotions p
WHERE 
    GETDATE() BETWEEN p.start_date AND p.end_date
    AND p.used_count < p.max_uses;


