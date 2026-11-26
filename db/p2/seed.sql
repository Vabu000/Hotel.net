-- Project 2: Idempotent Seed Data for Hotel Catalog

INSERT INTO roomcategories (categoryid, name, description, baseprice)
VALUES
    (1, 'Standard', 'Comfortable standard hotel room with essential amenities', 120.00),
    (2, 'Deluxe', 'Spacious deluxe room with city view and king-size bed', 210.00),
    (3, 'Suite', 'Luxury suite with separate living area and panoramic view', 360.00)
ON CONFLICT (categoryid) DO NOTHING;

INSERT INTO roomcategorydetails (categoryid, maxoccupancy, area, bedtype)
VALUES
    (1, 2, 24.5, 'Queen'),
    (2, 3, 35.0, 'King'),
    (3, 4, 58.0, 'King + Sofa Bed')
ON CONFLICT (categoryid) DO NOTHING;

INSERT INTO services (serviceid, name, price)
VALUES
    (1, 'Breakfast Buffet', 25.00),
    (2, 'Airport Transfer', 45.00),
    (3, 'Spa Access', 60.00),
    (4, 'Late Checkout', 30.00)
ON CONFLICT (serviceid) DO NOTHING;

INSERT INTO roomservices (categoryid, serviceid, additionalprice)
VALUES
    (1, 1, 0.00),
    (1, 2, 5.00),
    (2, 1, 0.00),
    (2, 3, 10.00),
    (3, 1, 0.00),
    (3, 3, 0.00),
    (3, 4, 0.00)
ON CONFLICT (categoryid, serviceid) DO NOTHING;

INSERT INTO discountcategories (discountcategoryid, name, discountpercentage)
VALUES
    (1, 'Loyalty Silver', 5.00),
    (2, 'Loyalty Gold', 12.00),
    (3, 'Corporate Partner', 15.00)
ON CONFLICT (discountcategoryid) DO NOTHING;

INSERT INTO clientdiscounts (clientid, discountcategoryid)
VALUES
    (1, 2),
    (2, 1)
ON CONFLICT (clientid, discountcategoryid) DO NOTHING;

SELECT setval(pg_get_serial_sequence('roomcategories', 'categoryid'), COALESCE((SELECT MAX(categoryid) FROM roomcategories), 1));
SELECT setval(pg_get_serial_sequence('services', 'serviceid'), COALESCE((SELECT MAX(serviceid) FROM services), 1));
SELECT setval(pg_get_serial_sequence('discountcategories', 'discountcategoryid'), COALESCE((SELECT MAX(discountcategoryid) FROM discountcategories), 1));
