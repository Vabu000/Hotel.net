-- The booking-time rate is an immutable snapshot. Legacy records remain NULL
-- because their historical rates cannot be reconstructed reliably.
ALTER TABLE reservations
    ADD COLUMN IF NOT EXISTS room_rate_snapshot numeric(12,2)
    CHECK (room_rate_snapshot IS NULL OR room_rate_snapshot >= 0);
