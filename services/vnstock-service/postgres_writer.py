"""Batch-write tick data to PostgreSQL ticks table."""
import asyncio
import logging
from typing import TYPE_CHECKING

import asyncpg

if TYPE_CHECKING:
    from tick_normalizer import TickData

logger = logging.getLogger(__name__)


class BatchWriter:
    """
    Buffers tick data in memory and flushes to PostgreSQL every N seconds.
    Uses asyncpg for high-throughput batch inserts.
    """

    def __init__(self, dsn: str, flush_interval: float = 5.0) -> None:
        self._dsn = dsn
        self._flush_interval = flush_interval
        self._buffer: list["TickData"] = []
        self._pool: "asyncpg.Pool | None" = None

    async def setup(self) -> None:
        """Create asyncpg connection pool."""
        self._pool = await asyncpg.create_pool(self._dsn, min_size=1, max_size=5)
        logger.info("PostgreSQL connection pool created")

    def buffer(self, ticks: "list[TickData]") -> None:
        """Add ticks to the in-memory buffer."""
        self._buffer.extend(ticks)

    async def flush_loop(self) -> None:
        """Background task: flush buffer to DB every N seconds."""
        while True:
            await asyncio.sleep(self._flush_interval)
            if self._buffer:
                await self._flush()

    async def _flush(self) -> None:
        """Write buffered ticks to PostgreSQL and clear buffer."""
        if not self._pool:
            logger.warning("PostgreSQL pool not initialized, skipping flush")
            return

        batch = self._buffer.copy()
        self._buffer.clear()

        try:
            async with self._pool.acquire() as conn:
                await conn.executemany(
                    """
                    INSERT INTO ticks (symbol, timestamp, price, volume, change_pct)
                    VALUES ($1, $2, $3, $4, $5)
                    ON CONFLICT DO NOTHING
                    """,
                    [
                        (tick.symbol, tick.timestamp, tick.price, tick.volume, tick.change_pct)
                        for tick in batch
                    ]
                )
            logger.debug("Flushed %d ticks to PostgreSQL", len(batch))
        except Exception as e:
            logger.error("PostgreSQL flush failed: %s — re-buffering %d ticks", e, len(batch))
            self._buffer[:0] = batch  # re-add to front on failure

    async def close(self) -> None:
        """Close the connection pool."""
        if self._pool:
            await self._pool.close()
