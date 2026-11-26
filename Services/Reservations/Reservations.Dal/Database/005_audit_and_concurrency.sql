-- Add consistent audit metadata to every Reservation-context table.
DO $$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'clients', 'rooms', 'reservations', 'reservation_details',
        'reservation_status_history', 'payments', 'catalog_service_snapshots', 'reservation_services'
    ] LOOP
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS createdat timestamptz NOT NULL DEFAULT now()', table_name);
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS createdby varchar(100) NULL', table_name);
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS updatedat timestamptz NULL', table_name);
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS updatedby varchar(100) NULL', table_name);
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS isdeleted boolean NOT NULL DEFAULT false', table_name);
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1 CHECK (version > 0)', table_name);
    END LOOP;
END $$;

CREATE INDEX IF NOT EXISTS ix_clients_active_email ON clients (email) WHERE isdeleted = false;
CREATE INDEX IF NOT EXISTS ix_rooms_active_number ON rooms (roomnumber) WHERE isdeleted = false;
