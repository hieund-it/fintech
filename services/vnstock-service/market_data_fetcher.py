"""
Poll vnstock (TCBS) for live price data.

Note: vnstock uses HTTP polling (not persistent WebSocket).
Poll interval of 3s satisfies the <=5s latency requirement.
"""
import asyncio
import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from tick_normalizer import TickData
    from redis_publisher import RedisPublisher
    from postgres_writer import BatchWriter

from tick_normalizer import normalize

logger = logging.getLogger(__name__)


async def fetch_ticks(symbols: list[str]) -> "list[TickData]":
    """
    Fetch latest prices for a list of symbols via vnstock.
    Runs in executor to avoid blocking the event loop (vnstock is sync).
    """
    loop = asyncio.get_running_loop()
    return await loop.run_in_executor(None, _fetch_sync, symbols)


def _fetch_sync(symbols: list[str]) -> "list[TickData]":
    """Synchronous vnstock fetch — called in thread pool."""
    try:
        from vnstock import Vnstock  # type: ignore[import]
        stock = Vnstock().stock(symbol=symbols[0], source="VCI")
        raw_df = stock.trading.price_board(symbols_list=symbols)
        if raw_df is None or raw_df.empty:
            return []

        ticks = []
        for _, row in raw_df.iterrows():
            symbol = str(row.get("ticker", row.get("symbol", ""))).upper()
            if not symbol:
                continue
            tick = normalize(symbol, row.to_dict())
            if tick:
                ticks.append(tick)
        return ticks

    except Exception as e:
        logger.error("vnstock fetch error: %s", e)
        return []


async def poll_loop(
    symbols: list[str],
    publisher: "RedisPublisher",
    writer: "BatchWriter",
    poll_interval: float = 3.0,
) -> None:
    """
    Main polling loop: fetch -> publish to Redis -> buffer for PostgreSQL.
    Uses exponential backoff on consecutive errors (max 60s).
    """
    backoff = poll_interval
    error_streak = 0
    total_processed = 0
    log_counter = 0

    while True:
        try:
            ticks = await fetch_ticks(symbols)

            for tick in ticks:
                await publisher.publish(tick)

            writer.buffer(ticks)
            total_processed += len(ticks)
            error_streak = 0
            backoff = poll_interval

            # Log stats every 20 iterations (~60s at 3s interval)
            log_counter += 1
            if log_counter >= 20:
                logger.info("Polling stats: processed %d ticks total", total_processed)
                log_counter = 0

        except Exception as e:
            error_streak += 1
            backoff = min(backoff * 2, 60.0)
            logger.error("Poll error (streak=%d, backoff=%.1fs): %s", error_streak, backoff, e)
            await asyncio.sleep(backoff)
            continue

        await asyncio.sleep(poll_interval)
