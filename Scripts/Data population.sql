
-- 1. Insert into users
INSERT INTO users (email, password_hash, first_name, last_name, date_of_birth, gender, profile_picture_url, phone_number, account_status, email_verified, phone_verified, created_at, updated_at, last_login, role)
VALUES 
    ('john.doe@example.com', 'hash123', 'John', 'Doe', '1990-05-15', 'male', 'https://example.com/john.jpg', '123-456-7890', 'active', 1, 1, GETDATE(), GETDATE(), GETDATE(), 'host'),
    ('jane.smith@example.com', 'hash456', 'Jane', 'Smith', '1985-08-22', 'female', 'https://example.com/jane.jpg', '234-567-8901', 'active', 1, 0, GETDATE(), GETDATE(), GETDATE(), 'guest'),
    ('admin@example.com', 'hash789', 'Admin', 'User', '1980-01-01', 'others', 'https://example.com/admin.jpg', '345-678-9012', 'active', 1, 1, GETDATE(), GETDATE(), GETDATE(), 'admin'),
    ('mary.jones@example.com', 'hash101', 'Mary', 'Jones', '1992-11-30', 'female', 'https://example.com/mary.jpg', '456-789-0123', 'pending', 0, 0, GETDATE(), GETDATE(), NULL, 'guest');

-- 2. Insert into host
INSERT INTO [host] ([user_id], [start_date], [my_work], [pets], [education], [funfact], [languages], [is_verified])
VALUES 
    (1, '2023-01-01', 'Software Engineer', 'Dog named Max', 'BS Computer Science', 'I love hiking!', 'English, Spanish', 1),
    (3, '2022-06-15', 'System Administrator', 'None', 'MS IT', 'I’ve been to 20 countries.', 'English', 0);

-- 3. Insert into cancellation_policies
INSERT INTO cancellation_policies (name, description)
VALUES 
    ('flexible', 'Full refund if canceled 24 hours before check-in.'),
    ('strict', 'No refund within 7 days of check-in.');

-- 4. Insert into properties
INSERT INTO properties (host_id, title, description, property_type, address, city, state, postal_code, latitude, longitude, price_per_night, cleaning_fee, service_fee, min_nights, max_nights, bedrooms, bathrooms, max_guests, check_in_time, check_out_time, instant_book, status, created_at, updated_at, cancellation_policy_id)
VALUES 
    (1, 'Cozy Downtown Apartment', 'A lovely 1-bedroom apartment in the heart of the city.', 'Apartment', '123 Main St', 'New York', 'NY', '10001', 40.7128, -74.0060, 120.00, 20.00, 15.00, 1, 30, 1, 1, 2, '15:00', '11:00', 1, 'active', GETDATE(), GETDATE(), 1),
    (1, 'Spacious Lake House', 'Relax by the lake in this 3-bedroom retreat.', 'House', '456 Lake Rd', 'Austin', 'TX', '73301', 30.2672, -97.7431, 200.00, 30.00, 25.00, 2, 60, 3, 2, 6, '14:00', '12:00', 0, 'active', GETDATE(), GETDATE(), 2),
    (2, 'Modern Loft', 'Stylish loft with city views.', 'Loft', '789 Sky St', 'San Francisco', 'CA', '94102', 37.7749, -122.4194, 150.00, 25.00, 20.00, 1, 45, 2, 1, 4, '16:00', '10:00', 1, 'pending', GETDATE(), GETDATE(), 1);

-- 5. Insert into property_images
INSERT INTO property_images (property_id, image_url, is_primary, category, created_at)
VALUES 
    (1, 'https://example.com/apt1_main.jpg', 1, 'Bedroom', GETDATE()),
    (1, 'https://example.com/apt1_bath.jpg', 0, 'Bathroom', GETDATE()),
    (2, 'https://example.com/lake_main.jpg', 1, 'Additional', GETDATE()),
    (3, 'https://example.com/loft_main.jpg', 1, 'Bedroom', GETDATE());

-- 6. Insert into favourite
INSERT INTO favourite (user_id, property_id)
VALUES 
    (2, 1),
    (2, 3),
    (4, 2);

-- 7. Insert into amenities
INSERT INTO amenities (name, category, icon_url)
VALUES 
    ('Wi-Fi', 'Basic', 'https://example.com/wifi_icon.png'),
    ('Pool', 'Outdoor', 'https://example.com/pool_icon.png'),
    ('Air Conditioning', 'Basic', 'https://example.com/ac_icon.png'),
    ('Kitchen', 'Facilities', 'https://example.com/kitchen_icon.png');

-- 8. Insert into property_amenities
INSERT INTO property_amenities (property_id, amenity_id, created_at)
VALUES 
    (1, 1, GETDATE()), -- Wi-Fi for Apartment
    (1, 3, GETDATE()), -- AC for Apartment
    (2, 2, GETDATE()), -- Pool for Lake House
    (2, 4, GETDATE()), -- Kitchen for Lake House
    (3, 1, GETDATE()), -- Wi-Fi for Loft
    (3, 3, GETDATE()); -- AC for Loft

-- 9. Insert into bookings
INSERT INTO bookings (property_id, guest_id, start_date, end_date, status)
VALUES 
    (1, 2, '2025-04-10', '2025-04-12', 'confirmed'),
    (2, 4, '2025-05-01', '2025-05-05', 'pending'),
    (3, 2, '2025-06-15', '2025-06-18', 'denied');

-- 10. Insert into booking_payments
INSERT INTO booking_payments (booking_id, amount, payment_method_type, status, transaction_id, created_at, updated_at, payout_method_type, paid_out)
VALUES 
    (1, 275.00, 'paypal', 'completed', 'TXN123', GETDATE(), GETDATE(), 'wire_transfer', 1),
    (2, 880.00, 'bank_account', 'pending', 'TXN456', GETDATE(), GETDATE(), 'payinooer', 0);

-- 11. Insert into reviews
INSERT INTO reviews (booking_id, reviewer_id, rating, comment, created_at, updated_at)
VALUES 
    (1, 2, 4, 'Great stay, very clean!', GETDATE(), GETDATE());

-- 12. Insert into host_verifications
INSERT INTO host_verifications (user_id, type, status, document_url, submitted_at, verified_at)
VALUES 
    (1, 'ID', 'verified', 'https://example.com/john_id.jpg', GETDATE(), GETDATE()),
    (2, 'ID', 'pending', 'https://example.com/admin_id.jpg', GETDATE(), NULL);

-- 13. Insert into promotions
INSERT INTO promotions (code, discount_type, amount, start_date, end_date, max_uses, used_count, created_at)
VALUES 
    ('SUMMER25', 'percentage', 25.00, '2025-06-01', '2025-08-31', 100, 0, GETDATE()),
    ('FIRST10', 'fixed', 10.00, '2025-04-01', '2025-12-31', 50, 1, GETDATE());

-- 14. Insert into user_used_promotions
INSERT INTO user_used_promotions (promotions_id, booking_id, user_id)
VALUES 
    (2, 1, 2); -- Jane used FIRST10 for her booking

-- 15. Insert into conversations
INSERT INTO conversations (created_at)
VALUES 
    (GETDATE()),
    (GETDATE());

-- 16. Insert into messages
INSERT INTO messages (conversation_id, sender_id, receiver_id, content, sent_at, read_at)
VALUES 
    (1, 2, 1, 'Hi, is the apartment available next week?', GETDATE(), NULL),
    (1, 1, 2, 'Yes, it’s available! Let me know if you want to book.', DATEADD(minute, 5, GETDATE()), GETDATE()),
    (2, 4, 1, 'Can I bring my dog to the lake house?', GETDATE(), NULL);


-- Add Foreign Key Constraints
ALTER TABLE [host] 
ADD FOREIGN KEY ([user_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [properties] 
ADD FOREIGN KEY ([host_id]) REFERENCES [host] ([id]);
GO

ALTER TABLE [properties] 
ADD FOREIGN KEY ([cancellation_policy_id]) REFERENCES [cancellation_policies] ([id]);
GO

ALTER TABLE [property_images] 
ADD FOREIGN KEY ([property_id]) REFERENCES [properties] ([id]);
GO

ALTER TABLE [favourite] 
ADD FOREIGN KEY ([user_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [favourite] 
ADD FOREIGN KEY ([property_id]) REFERENCES [properties] ([id]);
GO

ALTER TABLE [property_amenities] 
ADD FOREIGN KEY ([property_id]) REFERENCES [properties] ([id]);
GO

ALTER TABLE [property_amenities] 
ADD FOREIGN KEY ([amenity_id]) REFERENCES [amenities] ([id]);
GO

ALTER TABLE [bookings] 
ADD FOREIGN KEY ([property_id]) REFERENCES [properties] ([id]);
GO

ALTER TABLE [bookings] 
ADD FOREIGN KEY ([guest_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [booking_payments] 
ADD FOREIGN KEY ([booking_id]) REFERENCES [bookings] ([id]);
GO

ALTER TABLE [reviews] 
ADD FOREIGN KEY ([booking_id]) REFERENCES [bookings] ([id]);
GO

ALTER TABLE [reviews] 
ADD FOREIGN KEY ([reviewer_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [host_verifications] 
ADD FOREIGN KEY ([user_id]) REFERENCES [host] ([id]);
GO

ALTER TABLE [user_used_promotions] 
ADD FOREIGN KEY ([promotions_id]) REFERENCES [promotions] ([id]);
GO

ALTER TABLE [user_used_promotions] 
ADD FOREIGN KEY ([booking_id]) REFERENCES [bookings] ([id]);
GO

ALTER TABLE [user_used_promotions] 
ADD FOREIGN KEY ([user_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [messages] 
ADD FOREIGN KEY ([conversation_id]) REFERENCES [conversations] ([id]);
GO

ALTER TABLE [messages] 
ADD FOREIGN KEY ([sender_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [messages] 
ADD FOREIGN KEY ([receiver_id]) REFERENCES [users] ([id]);
GO

-- Create Indexes
CREATE INDEX [idx_users_email] ON [users] ([email]);
GO

CREATE INDEX [idx_properties_location] ON [properties] ([city], [state], [postal_code]);
GO

CREATE INDEX [idx_bookings_dates] ON [bookings] ([property_id], [start_date], [end_date]);
GO

-- Verify data insertion

use Airbnb_clone
SELECT 'users', COUNT(*) FROM users
UNION ALL
SELECT 'host', COUNT(*) FROM host
UNION ALL
SELECT 'properties', COUNT(*) FROM properties
UNION ALL
SELECT 'bookings', COUNT(*) FROM bookings;