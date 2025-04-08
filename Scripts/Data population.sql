use Airbnb

-- Insert into Users table (Guests and potential Hosts)
INSERT INTO Users (
    email, password_hash, first_name, last_name, date_of_birth, phone_number, 
    account_status, role, updated_at
)
VALUES 
    ('john.doe@example.com', 'AQAAAAIAAYagAAAAENy5KIk2bSHUDVGgpsA1+RA8U6hFQsLd8lipnJeGs9z33g0Y1bpn7PhbRUTzDFXiVA==', 'John', 'Doe', '1990-05-15', '+12345678901', 'active', 'Guest', GETDATE()),
    ('jane.smith@example.com', 'AQAAAAIAAYagAAAAENy5KIk2bSHUDVGgpsA1+RA8U6hFQsLd8lipnJeGs9z33g0Y1bpn7PhbRUTzDFXiVA==', 'Jane', 'Smith', '1985-08-22', '+12345678902', 'active', 'Host', GETDATE()),
    ('bob.johnson@example.com', 'AQAAAAIAAYagAAAAENy5KIk2bSHUDVGgpsA1+RA8U6hFQsLd8lipnJeGs9z33g0Y1bpn7PhbRUTzDFXiVA==', 'Bob', 'Johnson', '1992-03-10', '+12345678903', 'active', 'Guest', GETDATE()),
    ('admin@example.com', 'AQAAAAIAAYagAAAAENy5KIk2bSHUDVGgpsA1+RA8U6hFQsLd8lipnJeGs9z33g0Y1bpn7PhbRUTzDFXiVA==', 'Admin', 'User', '1980-01-01', '+12345678904', 'active', 'Admin', GETDATE());

-- Insert into hosts table (linked to Users with role 'host')
-- Use ID 2, which corresponds to Jane Smith (the host from Users)
INSERT INTO hosts (host_id, about_me, work, education, languages)
VALUES 
    (2, 'Friendly host who loves meeting new people', 'Software Engineer', 'BS Computer Science', 'English, Spanish');

-- Insert into PropertyCategories
INSERT INTO PropertyCategories (name, description, icon_url)
VALUES 
    ('Apartment', 'Cozy urban living', 'https://example.com/icons/apartment.png'),
    ('House', 'Spacious family home', 'https://example.com/icons/house.png');

-- Insert into CancellationPolicies
INSERT INTO CancellationPolicies (name, description, refund_percentage)
VALUES 
    ('flexible', 'Full refund 24 hours before', 100.00),
    ('moderate', 'Partial refund 5 days before', 50.00),
    ('strict', 'No refund within 7 days', 0.00);

-- Insert into Amenities
INSERT INTO Amenities (name, category, icon_url)
VALUES 
    ('WiFi', 'Basic', 'https://example.com/icons/wifi.png'),
    ('Pool', 'Outdoor', 'https://example.com/icons/pool.png'),
    ('Kitchen', 'Basic', 'https://example.com/icons/kitchen.png');

-- Insert into Properties (linked to hosts and cancellation policies)
INSERT INTO Properties (
    host_id, category_id, title, description, property_type, country, 
    address, city, postal_code, latitude, longitude, currency, 
    price_per_night, min_nights, max_nights, bedrooms, bathrooms, 
    max_guests, cancellation_policy_id, status, updated_at
)
VALUES 
    (
        2, 1, 'Cozy Downtown Apartment', 'Modern apartment in city center', 
        'Apartment', 'USA', '123 Main St', 'New York', '10001', 
        40.7128, -74.0060, 'USD', 150.00, 2, 30, 2, 1, 4, 1, 'active', GETDATE()
    ),
    (
        2, 2, 'Spacious Suburban House', 'Family home with large backyard', 
        'House', 'USA', '456 Oak Ave', 'Los Angeles', '90001', 
        34.0522, -118.2437, 'USD', 250.00, 3, 60, 3, 2, 6, 2, 'active', GETDATE()
    );



-- Insert into PropertyAmenities (junction table)
INSERT INTO PropertyAmenities (AmenitiesId, PropertiesId)
VALUES 
    (1, 1), -- WiFi for Apartment
    (3, 1), -- Kitchen for Apartment
    (1, 2), -- WiFi for House
    (2, 2), -- Pool for House
    (3, 2); -- Kitchen for House

-- Insert into PropertyImages
INSERT INTO PropertyImages (property_id, image_url, description, is_primary, category)
VALUES 
    (1, 'https://example.com/images/apt1-main.jpg', 'Living room', 1, 'Living Area'),
    (1, 'https://example.com/images/apt1-bed.jpg', 'Master bedroom', 0, 'Bedroom'),
    (2, 'https://example.com/images/house-main.jpg', 'Front view', 1, 'Exterior');

-- Insert into PropertyAvailabilities
INSERT INTO PropertyAvailabilities (property_id, date, is_available, price, min_nights)
VALUES 
    (1, '2025-05-01', 1, 150.00, 2),
    (1, '2025-05-02', 1, 150.00, 2),
    (2, '2025-05-01', 1, 250.00, 3);

-- Insert into Promotions
INSERT INTO Promotions (code, discount_type, amount, start_date, end_date, max_uses)
VALUES 
    ('SUMMER25', 'percentage', 25.00, '2025-06-01', '2025-08-31', 100),
    ('FIRSTBOOK', 'fixed', 50.00, '2025-04-08', '2025-12-31', 50);

-- Insert into Bookings (linked to Properties and Users)
INSERT INTO Bookings (
    property_id, guest_id, start_date, end_date, 
    check_in_status, check_out_status, status, total_amount, updated_at
)
VALUES 
    (1, 1, '2025-05-01', '2025-05-04', 'pending', 'pending', 'confirmed', 450.00, GETDATE()),
    (2, 3, '2025-05-01', '2025-05-05', 'pending', 'pending', 'pending', 1000.00, GETDATE());

-- Insert into BookingPayments
INSERT INTO BookingPayments (
    booking_id, amount, payment_method_type, status, transaction_id, updated_at
)
VALUES 
    (1, 450.00, 'credit_card', 'completed', 'txn_123456', GETDATE()),
    (2, 1000.00, 'paypal', 'pending', 'txn_789012', GETDATE());

-- Insert into Reviews
INSERT INTO Reviews (booking_id, reviewer_id, rating, comment, updated_at)
VALUES 
    (1, 1, 5, 'Great stay, very clean and comfortable!', GETDATE());
-- Insert into Favourites
INSERT INTO Favourites (user_id, property_id)
VALUES 
    (1, 2), -- John likes the House
    (3, 1); -- Bob likes the Apartment

-- Insert into Conversations
INSERT INTO Conversations (property_id, subject, user1_id, user2_id)
VALUES 
    (1, 'Booking Inquiry', 1, 2); -- John asking Jane about the apartment

-- Insert into Messages
INSERT INTO Messages (conversation_id, sender_id, content)
VALUES 
    (1, 1, 'Hi, is the apartment available for May 5th as well?'),
    (1, 2, 'Yes, it should be available. Let me double-check.');