-- Run monthly via cron to maintain ticks partitions
-- Creates next month partition + drops partitions older than 6 months
DO $$
DECLARE
    next_month DATE := date_trunc('month', NOW() + INTERVAL '1 month');
    next_month_end DATE := next_month + INTERVAL '1 month';
    partition_name TEXT := 'ticks_' || to_char(next_month, 'YYYY_MM');
    old_partition DATE := date_trunc('month', NOW() - INTERVAL '6 months');
    old_partition_name TEXT := 'ticks_' || to_char(old_partition, 'YYYY_MM');
BEGIN
    -- Create next month partition if not exists
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF ticks FOR VALUES FROM (%L) TO (%L)',
        partition_name, next_month, next_month_end
    );

    -- Drop partition older than 6 months (archive data first if needed)
    IF to_regclass(old_partition_name) IS NOT NULL THEN
        EXECUTE format('DROP TABLE IF EXISTS %I', old_partition_name);
        RAISE NOTICE 'Dropped old partition: %', old_partition_name;
    END IF;
END;
$$;
