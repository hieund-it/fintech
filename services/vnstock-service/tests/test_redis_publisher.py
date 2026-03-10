"""Unit tests for Redis publisher."""
import sys
import os

# Allow imports from parent service directory
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import asyncio
import json
import pytest
from unittest.mock import AsyncMock
from datetime import datetime, timezone
from tick_normalizer import TickData
from redis_publisher import RedisPublisher


@pytest.fixture
def sample_tick():
    return TickData(
        symbol="VCB",
        timestamp=datetime(2026, 3, 9, 10, 0, 0, tzinfo=timezone.utc),
        price=90000.0,
        volume=150000,
        change_pct=1.5
    )


@pytest.mark.asyncio
async def test_publish_calls_redis_with_correct_channel(sample_tick):
    """Publisher calls redis.publish with channel ticks:{symbol}."""
    mock_redis = AsyncMock()
    publisher = RedisPublisher(mock_redis)

    await publisher.publish(sample_tick)

    mock_redis.publish.assert_called_once()
    channel, payload = mock_redis.publish.call_args[0]
    assert channel == "ticks:VCB"


@pytest.mark.asyncio
async def test_publish_payload_contains_symbol(sample_tick):
    """Published JSON payload contains symbol and price fields."""
    mock_redis = AsyncMock()
    publisher = RedisPublisher(mock_redis)

    await publisher.publish(sample_tick)

    _, payload = mock_redis.publish.call_args[0]
    data = json.loads(payload)
    assert data["symbol"] == "VCB"
    assert data["price"] == 90000.0


@pytest.mark.asyncio
async def test_publish_handles_redis_error(sample_tick):
    """Publisher handles Redis connection errors gracefully without raising."""
    mock_redis = AsyncMock()
    mock_redis.publish.side_effect = Exception("Redis connection failed")
    publisher = RedisPublisher(mock_redis)

    # Should not raise
    await publisher.publish(sample_tick)
