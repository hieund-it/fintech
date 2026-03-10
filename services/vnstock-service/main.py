"""
VnStock Data Service — FastAPI entry point.

Polls TCBS via vnstock for live stock prices.
Publishes to Redis pub/sub and batch-writes to PostgreSQL.
"""
import asyncio
import json
import logging
import sys
from contextlib import asynccontextmanager

import redis.asyncio as aioredis
from fastapi import FastAPI

from config import settings
from market_data_fetcher import poll_loop
from postgres_writer import BatchWriter
from redis_publisher import RedisPublisher

logging.basicConfig(
    level=getattr(logging, settings.log_level.upper(), logging.INFO),
    format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
    stream=sys.stdout,
)
logger = logging.getLogger("main")


def _load_symbols() -> list[str]:
    """Load symbol list from JSON file."""
    try:
        with open(settings.symbols_file) as f:
            symbols = json.load(f)
        logger.info("Loaded %d symbols from %s", len(symbols), settings.symbols_file)
        return [s.upper() for s in symbols if isinstance(s, str)]
    except FileNotFoundError:
        logger.warning("symbols.json not found, using default VCB")
        return ["VCB"]


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Startup / shutdown lifecycle."""
    symbols = _load_symbols()

    # Redis client
    redis_client = aioredis.from_url(settings.redis_url, decode_responses=True)

    # PostgreSQL batch writer
    writer = BatchWriter(settings.postgres_dsn, settings.batch_persist_interval)
    await writer.setup()

    publisher = RedisPublisher(redis_client)

    # Start background tasks
    poll_task = asyncio.create_task(
        poll_loop(symbols, publisher, writer, settings.tcbs_poll_interval),
        name="poll_loop"
    )
    flush_task = asyncio.create_task(writer.flush_loop(), name="flush_loop")

    logger.info(
        "VnStock service started — polling %d symbols every %.1fs",
        len(symbols),
        settings.tcbs_poll_interval,
    )

    yield

    # Cleanup
    poll_task.cancel()
    flush_task.cancel()
    await asyncio.gather(poll_task, flush_task, return_exceptions=True)
    await writer.close()
    await redis_client.aclose()
    logger.info("VnStock service shut down cleanly")


app = FastAPI(title="VnStock Data Service", version="1.0.0", lifespan=lifespan)


@app.get("/health")
def health():
    """Health check endpoint for Docker healthcheck."""
    return {"status": "ok", "service": "vnstock-data-service"}
