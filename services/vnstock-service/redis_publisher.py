"""Publish normalized tick data to Redis pub/sub channels."""
import json
import logging
from redis.asyncio import Redis

from config import settings

logger = logging.getLogger(__name__)


class RedisPublisher:
    """Publishes tick data to Redis channel {prefix}:{symbol}."""

    def __init__(self, redis_client: Redis) -> None:
        self._redis = redis_client
        self._channel_prefix = settings.redis_channel_prefix

    async def publish(self, tick) -> None:
        """
        Publish a TickData to Redis channel `{prefix}:{symbol}`.

        Args:
            tick: TickData instance to publish
        """
        channel = f"{self._channel_prefix}:{tick.symbol}"
        payload = json.dumps(tick.to_dict())
        try:
            await self._redis.publish(channel, payload)
        except Exception as e:
            logger.error("Redis publish failed for %s: %s", tick.symbol, e)
