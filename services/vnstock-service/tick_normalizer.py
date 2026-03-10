"""Normalize raw vnstock API responses into TickData dataclass."""
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
import logging

logger = logging.getLogger(__name__)


@dataclass
class TickData:
    """Normalized tick data for a single stock at a point in time."""
    symbol: str
    timestamp: datetime
    price: float
    volume: int
    change_pct: float

    def to_dict(self) -> dict:
        """Convert to JSON-serializable dict."""
        d = asdict(self)
        d["timestamp"] = self.timestamp.isoformat()
        return d


def normalize(symbol: str, raw: dict) -> "TickData | None":
    """
    Map vnstock price board fields to TickData.

    Args:
        symbol: Stock ticker (e.g. 'VCB')
        raw: Raw dict from vnstock price_board() response

    Returns:
        TickData or None if raw data is invalid/missing required fields
    """
    try:
        # vnstock field names may vary by version — handle common variants
        price = _extract_price(raw)
        if price is None or price <= 0:
            logger.warning("Symbol %s: missing or zero price, skipping", symbol)
            return None

        volume = _extract_volume(raw)
        change_pct = _extract_change_pct(raw)

        return TickData(
            symbol=symbol,
            timestamp=datetime.now(timezone.utc),
            price=float(price),
            volume=int(volume),
            change_pct=float(change_pct),
        )
    except (KeyError, TypeError, ValueError) as e:
        logger.error("Failed to normalize tick for %s: %s | raw=%s", symbol, e, raw)
        return None


def _extract_price(raw: dict) -> "float | None":
    """Try common price field names from vnstock responses."""
    for field in ("price", "lastPrice", "last_price", "matchedPrice", "matched_price", "close"):
        val = raw.get(field)
        if val is not None:
            try:
                return float(val)
            except (ValueError, TypeError):
                continue
    return None


def _extract_volume(raw: dict) -> int:
    """Try common volume field names."""
    for field in ("volume", "totalVolume", "total_volume", "matchedVolume", "matched_volume"):
        val = raw.get(field)
        if val is not None:
            try:
                return int(float(val))
            except (ValueError, TypeError):
                continue
    return 0


def _extract_change_pct(raw: dict) -> float:
    """Try common change percentage field names."""
    for field in ("changePercent", "change_percent", "changePct", "change_pct", "pctChange"):
        val = raw.get(field)
        if val is not None:
            try:
                return float(val)
            except (ValueError, TypeError):
                continue
    return 0.0
