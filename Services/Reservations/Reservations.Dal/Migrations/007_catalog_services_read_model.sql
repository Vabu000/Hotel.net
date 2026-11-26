CREATE TABLE IF NOT EXISTS catalog_services (
    catalog_service_id integer PRIMARY KEY,
    name varchar(120) NOT NULL,
    price numeric(12,2) NOT NULL CHECK (price >= 0),
    updatedat timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_catalog_services_name ON catalog_services (name);
