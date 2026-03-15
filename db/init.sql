-- VnStock Database Schema — init.sql
-- Manages ONLY the ticks table (partitioned, Python-service owned).
-- EF Core migrations manage all other tables (Stocks, OhlcvDaily, Identity, etc.)

-- Tick data — partitioned by month for high-throughput writes
CREATE TABLE IF NOT EXISTS ticks (
    symbol VARCHAR(10) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    price DECIMAL(12,2) NOT NULL,
    volume BIGINT NOT NULL DEFAULT 0,
    change_pct DECIMAL(8,4)
) PARTITION BY RANGE (timestamp);

-- Create initial partitions (current + next 2 months)
CREATE TABLE IF NOT EXISTS ticks_2026_03 PARTITION OF ticks
    FOR VALUES FROM ('2026-03-01') TO ('2026-04-01');
CREATE TABLE IF NOT EXISTS ticks_2026_04 PARTITION OF ticks
    FOR VALUES FROM ('2026-04-01') TO ('2026-05-01');
CREATE TABLE IF NOT EXISTS ticks_2026_05 PARTITION OF ticks
    FOR VALUES FROM ('2026-05-01') TO ('2026-06-01');

CREATE INDEX IF NOT EXISTS idx_ticks_symbol_ts ON ticks (symbol, timestamp DESC);
