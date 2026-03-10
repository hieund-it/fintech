"""Unit tests for tick normalization logic."""
import sys
import os

# Allow imports from parent service directory
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import pytest
from tick_normalizer import normalize, TickData


def test_normalize_valid_data():
    """Standard vnstock price board row normalizes correctly."""
    raw = {"price": 90000, "volume": 150000, "changePercent": 1.5}
    result = normalize("VCB", raw)
    assert result is not None
    assert result.symbol == "VCB"
    assert result.price == 90000.0
    assert result.volume == 150000
    assert result.change_pct == 1.5


def test_normalize_lastprice_field():
    """Handles 'lastPrice' field variant."""
    raw = {"lastPrice": 45000.0, "totalVolume": 200000}
    result = normalize("HPG", raw)
    assert result is not None
    assert result.price == 45000.0


def test_normalize_none_price_returns_none():
    """Returns None when price is missing."""
    raw = {"volume": 100, "changePercent": 0.5}
    result = normalize("VCB", raw)
    assert result is None


def test_normalize_zero_price_returns_none():
    """Returns None when price is zero (invalid)."""
    raw = {"price": 0, "volume": 100}
    result = normalize("VCB", raw)
    assert result is None


def test_normalize_missing_volume_defaults_zero():
    """Missing volume field defaults to 0."""
    raw = {"price": 50000}
    result = normalize("FPT", raw)
    assert result is not None
    assert result.volume == 0


def test_normalize_missing_change_pct_defaults_zero():
    """Missing change_pct field defaults to 0.0."""
    raw = {"price": 50000, "volume": 1000}
    result = normalize("FPT", raw)
    assert result is not None
    assert result.change_pct == 0.0


def test_normalize_invalid_price_type_returns_none():
    """Non-numeric price returns None."""
    raw = {"price": "N/A", "volume": 100}
    result = normalize("VCB", raw)
    assert result is None


def test_tick_data_to_dict_serializable():
    """TickData.to_dict() returns JSON-serializable dict with ISO timestamp."""
    raw = {"price": 100000, "volume": 500000, "changePct": -0.5}
    tick = normalize("VNM", raw)
    assert tick is not None
    d = tick.to_dict()
    assert d["symbol"] == "VNM"
    assert isinstance(d["timestamp"], str)  # ISO string, not datetime object
    assert d["price"] == 100000.0
