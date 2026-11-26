INSERT INTO clients (fullname, passportdata, phone, email)
VALUES
    ('Olena Koval', 'AA000001', '+380501110001', 'olena.koval@example.test'),
    ('Dmytro Bondar', 'AA000002', '+380501110002', 'dmytro.bondar@example.test')
ON CONFLICT (email) DO NOTHING;

INSERT INTO rooms (roomnumber, capacity, price)
VALUES
    ('101', 2, 1800.00),
    ('201', 3, 2600.00),
    ('301', 4, 4200.00)
ON CONFLICT (roomnumber) DO NOTHING;

INSERT INTO reservations (clientid, roomid, room_rate_snapshot, checkindate, checkoutdate, status)
SELECT c.clientid, r.roomid, r.price, current_date + 14, current_date + 17, 0
FROM clients c
CROSS JOIN rooms r
WHERE c.email = 'olena.koval@example.test'
  AND r.roomnumber = '101'
  AND NOT EXISTS (
      SELECT 1
      FROM reservations existing
      WHERE existing.clientid = c.clientid
        AND existing.roomid = r.roomid
        AND existing.checkindate = current_date + 14
        AND existing.checkoutdate = current_date + 17
  );
