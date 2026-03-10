"""Publish normalized tick data to Redis pub/sub channels."""
import json
import logging
from redis.asyncio import Redis

logger = logging.getLogger(__name__)


class RedisPublisher:
    """Publishes tick data to Redis channel ticks:{symbol}."""

    def __init__(self, redis_client: Redis) -> None:
        self._redis = redis_client

    async def publish(self, tick) -> None:
        """
        Publish a TickData to Redis channel `ticks:{symbol}`.

        Args:
            tick: TickData instance to publish
        """
        channel = f"ticks:{tick.symbol}"
        payload = json.dumps(tick.to_dict())
        try:
            await self._redis.publish(channel, payload)
        except Exception as e:
            logger.error("Redis publish failed for %s: %s", tick.symbol, e)
