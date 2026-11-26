CREATE TABLE IF NOT EXISTS processed_events (
    event_id uuid PRIMARY KEY,
    event_type varchar(150) NOT NULL,
    processed_at timestamptz NOT NULL DEFAULT now()
);
