using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class addviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW vw_property_details AS
                SELECT 
                    p.id As property_id,
                    p.title,
                    p.description,
                    p.city,
                    p.price_per_night,
                    p.cleaning_fee,
                    p.service_fee,
                    p.property_type,
                    p.status,
                    h.host_id,
                    CONCAT(u.first_name, ' ', u.last_name) AS host_name, 
                    u.profile_picture_url AS host_picture,
                    COALESCE(AVG(r.rating), 0) AS average_rating, 
                    COUNT(r.id) AS review_count
                FROM 
                    properties p
                INNER JOIN 
                    hosts h ON p.host_id = h.host_id
                INNER JOIN 
                    users u ON h.host_id = u.id
                LEFT JOIN 
                    bookings b ON p.id = b.property_id
                LEFT JOIN 
                    reviews r ON b.id = r.booking_id
                GROUP BY 
                    p.id, p.title, p.description, p.city, p.price_per_night, 
                    p.cleaning_fee, p.service_fee, p.property_type, p.status, 
                    h.host_id, u.first_name, u.last_name, u.profile_picture_url;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_host_performance AS
                    SELECT 
                        h.host_id AS host_id,
                        CONCAT(u.first_name, ' ', u.last_name) AS host_name,
                        COUNT(DISTINCT p.id) AS total_properties,
                        COUNT(DISTINCT b.id) AS total_bookings,
                        AVG(r.rating) AS average_rating,
                        COUNT(r.id) AS total_reviews
                    FROM 
                        hosts h
                    INNER JOIN 
                        Users u ON h.host_id = u.id
                    LEFT JOIN 
                        Properties p ON h.host_id = p.host_id
                    LEFT JOIN 
                        Bookings b ON p.id = b.property_id AND b.status = 'confirmed'
                    LEFT JOIN 
                        Reviews r ON b.id = r.booking_id
                    GROUP BY 
                        h.host_id, u.first_name, u.last_name;");

            migrationBuilder.Sql(@"
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
                        Promotions p
                    WHERE 
                        GETDATE() BETWEEN p.start_date AND p.end_date
                        AND p.used_count < p.max_uses;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_property_details;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_host_performance;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_active_promotions;");

        }
    }
}
