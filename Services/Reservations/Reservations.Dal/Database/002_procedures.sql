CREATE OR REPLACE PROCEDURE usp_confirm_reservation(p_reservation_id integer)
LANGUAGE plpgsql
AS $$
DECLARE
    current_status integer;
BEGIN
    SELECT status
    INTO current_status
    FROM reservations
    WHERE reservationid = p_reservation_id AND isdeleted = false
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'RESERVATION_NOT_FOUND' USING ERRCODE = 'P0002';
    END IF;

    IF current_status <> 0 THEN
        RAISE EXCEPTION 'INVALID_RESERVATION_TRANSITION' USING ERRCODE = 'P0001';
    END IF;

    UPDATE reservations
    SET status = 1, updatedat = now(), version = version + 1
    WHERE reservationid = p_reservation_id;

    INSERT INTO reservation_status_history (reservationid, oldstatus, newstatus, changedby)
    VALUES (p_reservation_id, current_status, 1, current_user);
END;
$$;

CREATE OR REPLACE PROCEDURE usp_cancel_reservation(p_reservation_id integer)
LANGUAGE plpgsql
AS $$
DECLARE
    current_status integer;
BEGIN
    SELECT status
    INTO current_status
    FROM reservations
    WHERE reservationid = p_reservation_id AND isdeleted = false
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'RESERVATION_NOT_FOUND' USING ERRCODE = 'P0002';
    END IF;

    IF current_status NOT IN (0, 1) THEN
        RAISE EXCEPTION 'INVALID_RESERVATION_TRANSITION' USING ERRCODE = 'P0001';
    END IF;

    UPDATE reservations
    SET status = 4, updatedat = now(), version = version + 1
    WHERE reservationid = p_reservation_id;

    INSERT INTO reservation_status_history (reservationid, oldstatus, newstatus, changedby)
    VALUES (p_reservation_id, current_status, 4, current_user);
END;
$$;

CREATE OR REPLACE PROCEDURE usp_record_payment(
    p_reservation_id integer,
    p_amount numeric(12,2),
    p_method varchar(40))
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_amount IS NULL OR p_amount <= 0 THEN
        RAISE EXCEPTION 'INVALID_PAYMENT_AMOUNT' USING ERRCODE = 'P0001';
    END IF;

    IF p_method IS NULL OR btrim(p_method) = '' THEN
        RAISE EXCEPTION 'INVALID_PAYMENT_METHOD' USING ERRCODE = 'P0001';
    END IF;

    INSERT INTO payments (reservationid, amount, method)
    SELECT reservationid, p_amount, p_method
    FROM reservations
    WHERE reservationid = p_reservation_id AND isdeleted = false;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'RESERVATION_NOT_FOUND' USING ERRCODE = 'P0002';
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE usp_update_room_rate(
    p_room_id integer,
    p_price numeric(12,2))
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_price IS NULL OR p_price < 0 THEN
        RAISE EXCEPTION 'INVALID_ROOM_PRICE' USING ERRCODE = 'P0001';
    END IF;

    UPDATE rooms
    SET price = p_price, updatedat = now(), version = version + 1
    WHERE roomid = p_room_id AND isdeleted = false;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'ROOM_NOT_FOUND' USING ERRCODE = 'P0002';
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE usp_delete_cancelled_reservation(p_reservation_id integer)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE reservations
    SET isdeleted = true, updatedat = now(), version = version + 1
    WHERE reservationid = p_reservation_id AND status = 4 AND isdeleted = false;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'CANCELLED_RESERVATION_NOT_FOUND' USING ERRCODE = 'P0002';
    END IF;
END;
$$;
