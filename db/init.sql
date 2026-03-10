-- VnStock Database Schema
-- Run automatically by Postgres on first init

-- Stocks metadata
CREATE TABLE IF NOT EXISTS stocks (
    symbol VARCHAR(10) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    exchange VARCHAR(10) NOT NULL CHECK (exchange IN ('HOSE', 'HNX', 'UPCOM')),
    sector VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tick data — partitioned by month for performance
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

-- Indexes on ticks
CREATE INDEX IF NOT EXISTS idx_ticks_symbol_ts ON ticks (symbol, timestamp DESC);

-- OHLCV daily data
CREATE TABLE IF NOT EXISTS ohlcv_daily (
    symbol VARCHAR(10) NOT NULL,
    date DATE NOT NULL,
    open DECIMAL(12,2) NOT NULL,
    high DECIMAL(12,2) NOT NULL,
    low DECIMAL(12,2) NOT NULL,
    close DECIMAL(12,2) NOT NULL,
    volume BIGINT NOT NULL DEFAULT 0,
    PRIMARY KEY (symbol, date)
);

CREATE INDEX IF NOT EXISTS idx_ohlcv_symbol_date ON ohlcv_daily (symbol, date DESC);

-- Seed some common stocks
INSERT INTO stocks (symbol, name, exchange, sector) VALUES
    ('VCB', 'Vietcombank', 'HOSE', 'Banking'),
    ('VIC', 'Vingroup', 'HOSE', 'Real Estate'),
    ('VHM', 'Vinhomes', 'HOSE', 'Real Estate'),
    ('HPG', 'Hoa Phat Group', 'HOSE', 'Steel'),
    ('BID', 'BIDV', 'HOSE', 'Banking'),
    ('CTG', 'VietinBank', 'HOSE', 'Banking'),
    ('MBB', 'MB Bank', 'HOSE', 'Banking'),
    ('TCB', 'Techcombank', 'HOSE', 'Banking'),
    ('ACB', 'ACB Bank', 'HOSE', 'Banking'),
    ('VPB', 'VPBank', 'HOSE', 'Banking'),
    ('FPT', 'FPT Corporation', 'HOSE', 'Technology'),
    ('MWG', 'Mobile World', 'HOSE', 'Retail'),
    ('GAS', 'PV Gas', 'HOSE', 'Oil & Gas'),
    ('SAB', 'Sabeco', 'HOSE', 'Consumer Goods'),
    ('PLX', 'Petrolimex', 'HOSE', 'Oil & Gas')
ON CONFLICT (symbol) DO NOTHING;
