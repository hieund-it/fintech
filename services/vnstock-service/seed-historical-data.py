"""
One-time script to seed historical OHLCV data and stock metadata into PostgreSQL.

Usage:
    python seed-historical-data.py [--symbols top50.json] [--years 2]

The script:
1. Reads symbol list from a JSON file (or uses a built-in top-50 default).
2. Fetches daily OHLCV history via vnstock for each symbol.
3. Upserts Stock rows and inserts OhlcvDaily rows into PostgreSQL.
4. Commits after each symbol — safe to interrupt and re-run (idempotent).
5. Auto-detects rate-limit errors and waits before retrying (max 3 attempts).
"""

import argparse
import json
import os
import sys
import time
from datetime import date, timedelta

import psycopg2
import psycopg2.extras

try:
    from vnstock import Vnstock  # vnstock >= 3.x
except ImportError:
    print("vnstock not installed. Run: pip install vnstock")
    sys.exit(1)

# Rate-limit: free plan = 20 req/min. Wait 65s before retrying.
_RATE_LIMIT_WAIT = 65
_MAX_RETRIES = 3

# Keywords in exception messages that indicate rate-limiting
_RATE_LIMIT_SIGNALS = ("rate", "429", "wait", "retry", "limit", "request")

# Default top-50 HOSE stocks used when no --symbols file is provided
DEFAULT_SYMBOLS = [
    {"symbol": "VCB",  "name": "Vietcombank",                    "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "BID",  "name": "BIDV",                           "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "CTG",  "name": "VietinBank",                     "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "TCB",  "name": "Techcombank",                    "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "MBB",  "name": "Military Bank",                  "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "VPB",  "name": "VPBank",                         "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "ACB",  "name": "Asia Commercial Bank",           "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "HDB",  "name": "HDBank",                         "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "STB",  "name": "Sacombank",                      "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "VIB",  "name": "Vietnam International Bank",     "exchange": "HOSE", "sector": "Banking"},
    {"symbol": "VHM",  "name": "Vinhomes",                       "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "NVL",  "name": "Novaland",                       "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "PDR",  "name": "Phat Dat Real Estate",           "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "DXG",  "name": "Dat Xanh Group",                 "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "KDH",  "name": "Khang Dien House",               "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "VNM",  "name": "Vinamilk",                       "exchange": "HOSE", "sector": "Consumer Staples"},
    {"symbol": "SAB",  "name": "Sabeco",                         "exchange": "HOSE", "sector": "Consumer Staples"},
    {"symbol": "MSN",  "name": "Masan Group",                    "exchange": "HOSE", "sector": "Consumer Staples"},
    {"symbol": "MCH",  "name": "Masan Consumer Holdings",        "exchange": "HOSE", "sector": "Consumer Staples"},
    {"symbol": "PNJ",  "name": "Phu Nhuan Jewelry",              "exchange": "HOSE", "sector": "Consumer Discretionary"},
    {"symbol": "MWG",  "name": "Mobile World",                   "exchange": "HOSE", "sector": "Consumer Discretionary"},
    {"symbol": "FPT",  "name": "FPT Corporation",                "exchange": "HOSE", "sector": "Technology"},
    {"symbol": "CMG",  "name": "CMC Corporation",                "exchange": "HOSE", "sector": "Technology"},
    {"symbol": "VGI",  "name": "Viettel Global Investment",      "exchange": "HOSE", "sector": "Technology"},
    {"symbol": "GAS",  "name": "PetroVietnam Gas",               "exchange": "HOSE", "sector": "Energy"},
    {"symbol": "PLX",  "name": "Petrolimex",                     "exchange": "HOSE", "sector": "Energy"},
    {"symbol": "PVD",  "name": "PetroVietnam Drilling",          "exchange": "HOSE", "sector": "Energy"},
    {"symbol": "BSR",  "name": "Binh Son Refinery",              "exchange": "HOSE", "sector": "Energy"},
    {"symbol": "HPG",  "name": "Hoa Phat Group",                 "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "HSG",  "name": "Hoa Sen Group",                  "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "NKG",  "name": "Nam Kim Steel",                  "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "VIC",  "name": "Vingroup",                       "exchange": "HOSE", "sector": "Conglomerate"},
    {"symbol": "VRE",  "name": "Vincom Retail",                  "exchange": "HOSE", "sector": "Real Estate"},
    {"symbol": "HVN",  "name": "Vietnam Airlines",               "exchange": "HOSE", "sector": "Industrials"},
    {"symbol": "GMD",  "name": "Gemadept",                       "exchange": "HOSE", "sector": "Industrials"},
    {"symbol": "HAH",  "name": "Hai An Transport",               "exchange": "HOSE", "sector": "Industrials"},
    {"symbol": "DPM",  "name": "PetroVietnam Fertilizer",        "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "DCM",  "name": "Ca Mau Fertilizer",              "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "BMP",  "name": "Binh Minh Plastics",             "exchange": "HOSE", "sector": "Materials"},
    {"symbol": "REE",  "name": "Refrigeration Electrical Engineering", "exchange": "HOSE", "sector": "Industrials"},
    {"symbol": "PC1",  "name": "Power Construction 1",           "exchange": "HOSE", "sector": "Utilities"},
    {"symbol": "POW",  "name": "PetroVietnam Power",             "exchange": "HOSE", "sector": "Utilities"},
    {"symbol": "NT2",  "name": "PetroVietnam Nhon Trach 2",      "exchange": "HOSE", "sector": "Utilities"},
    {"symbol": "EVF",  "name": "EVN Finance",                    "exchange": "HOSE", "sector": "Financials"},
    {"symbol": "SSI",  "name": "SSI Securities",                 "exchange": "HOSE", "sector": "Financials"},
    {"symbol": "HCM",  "name": "Ho Chi Minh City Securities",    "exchange": "HOSE", "sector": "Financials"},
    {"symbol": "VND",  "name": "VNDirect Securities",            "exchange": "HOSE", "sector": "Financials"},
    {"symbol": "SHS",  "name": "Saigon-Hanoi Securities",        "exchange": "HNX",  "sector": "Financials"},
    {"symbol": "VCI",  "name": "Viet Capital Securities",        "exchange": "HOSE", "sector": "Financials"},
    {"symbol": "PHR",  "name": "Phuoc Hoa Rubber",               "exchange": "HOSE", "sector": "Materials"},
]


def get_db_connection() -> psycopg2.extensions.connection:
    """Build PostgreSQL DSN from individual env vars (same as vnstock-service config.py)."""
    from urllib.parse import quote_plus
    host = os.getenv("POSTGRES_HOST", "localhost")
    port = os.getenv("POSTGRES_PORT", "5433")
    db   = os.getenv("POSTGRES_DB",   "vnstock")
    user = os.getenv("POSTGRES_USER", "postgres")
    pwd  = os.getenv("POSTGRES_PASSWORD", "")
    dsn  = f"postgresql://{quote_plus(user)}:{quote_plus(pwd)}@{host}:{port}/{db}"
    return psycopg2.connect(dsn)


def upsert_stocks(conn, symbols: list[dict]) -> None:
    """Insert or update stock metadata rows and commit immediately."""
    sql = """
        INSERT INTO "Stocks" ("Symbol", "Name", "Exchange", "Sector")
        VALUES %s
        ON CONFLICT ("Symbol") DO UPDATE
            SET "Name" = EXCLUDED."Name",
                "Exchange" = EXCLUDED."Exchange",
                "Sector" = EXCLUDED."Sector"
    """
    values = [(s["symbol"], s["name"], s["exchange"], s.get("sector")) for s in symbols]
    with conn.cursor() as cur:
        psycopg2.extras.execute_values(cur, sql, values)
    conn.commit()
    print(f"  Upserted {len(values)} stock rows.")


def _fetch_ohlcv(symbol: str, start_date: str, end_date: str) -> list[tuple]:
    """Fetch OHLCV from vnstock API and return list of DB-ready row tuples."""
    stock = Vnstock().stock(symbol=symbol, source="VCI")
    df = stock.quote.history(start=start_date, end=end_date, interval="1D")

    if df is None or df.empty:
        return []

    rows = []
    for _, row in df.iterrows():
        rows.append((
            symbol,
            str(row.get("time", row.get("date", ""))),
            float(row.get("open", 0)),
            float(row.get("high", 0)),
            float(row.get("low", 0)),
            float(row.get("close", 0)),
            int(row.get("volume", 0)),
        ))
    return rows


def _insert_ohlcv(conn, rows: list[tuple]) -> None:
    """Bulk-upsert OHLCV rows and commit."""
    sql = """
        INSERT INTO "OhlcvDaily" ("Symbol", "Date", "Open", "High", "Low", "Close", "Volume")
        VALUES %s
        ON CONFLICT ("Symbol", "Date") DO UPDATE
            SET "Open"   = EXCLUDED."Open",
                "High"   = EXCLUDED."High",
                "Low"    = EXCLUDED."Low",
                "Close"  = EXCLUDED."Close",
                "Volume" = EXCLUDED."Volume"
    """
    with conn.cursor() as cur:
        psycopg2.extras.execute_values(cur, sql, rows)
    conn.commit()


def _is_rate_limit_error(exc: BaseException) -> bool:
    """Return True if the exception looks like a rate-limit or quota error.
    vnstock calls sys.exit(1) on rate-limit, so we also check SystemExit.
    """
    if isinstance(exc, SystemExit):
        return True
    msg = str(exc).lower()
    return any(signal in msg for signal in _RATE_LIMIT_SIGNALS)


def fetch_and_persist_symbol(
    conn, symbol: str, start_date: str, end_date: str
) -> int:
    """
    Fetch OHLCV for one symbol with auto-retry on rate-limit.

    Strategy:
    - On rate-limit: wait _RATE_LIMIT_WAIT seconds, then retry (max _MAX_RETRIES times)
    - On success: insert + commit immediately so data is never lost
    - On non-rate-limit error: skip the symbol
    """
    for attempt in range(1, _MAX_RETRIES + 1):
        try:
            rows = _fetch_ohlcv(symbol, start_date, end_date)
            if not rows:
                print(f"[SKIP] No data")
                return 0
            _insert_ohlcv(conn, rows)
            return len(rows)

        except Exception as exc:  # catches rate-limit errors; KeyboardInterrupt propagates normally
            if _is_rate_limit_error(exc):
                wait = _RATE_LIMIT_WAIT * attempt  # 65s, 130s, 195s
                print(
                    f"\n  [RATE LIMIT] attempt {attempt}/{_MAX_RETRIES}. "
                    f"Waiting {wait}s before retry...",
                    flush=True,
                )
                time.sleep(wait)
            else:
                print(f"[FAIL] {exc}")
                return 0

    print(f"[FAIL] Gave up after {_MAX_RETRIES} retries.")
    return 0


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed historical OHLCV data")
    parser.add_argument("--symbols", default=None, help="Path to JSON file with symbol list")
    parser.add_argument("--years", type=int, default=2, help="Years of history to fetch (default: 2)")
    args = parser.parse_args()

    # Load symbol list
    if args.symbols:
        with open(args.symbols) as f:
            symbols = json.load(f)
        print(f"Loaded {len(symbols)} symbols from {args.symbols}")
    else:
        symbols = DEFAULT_SYMBOLS
        print(f"Using built-in default list ({len(symbols)} symbols)")

    end_date = date.today()
    # --years 0 means from start of current year
    start_date = date(end_date.year, 1, 1) if args.years == 0 else end_date - timedelta(days=365 * args.years)
    start_str = start_date.strftime("%Y-%m-%d")
    end_str = end_date.strftime("%Y-%m-%d")
    print(f"Date range: {start_str} to {end_str}")

    conn = get_db_connection()
    try:
        # Upsert stock metadata in one committed batch
        print("\n[1/2] Upserting stock metadata...")
        upsert_stocks(conn, symbols)

        # Fetch + commit each symbol independently — safe to interrupt and re-run
        print("\n[2/2] Fetching OHLCV history (commit per symbol)...")
        total_rows = 0
        skipped = []

        for i, s in enumerate(symbols, 1):
            sym = s["symbol"]
            print(f"  ({i}/{len(symbols)}) {sym}... ", end="", flush=True)
            n = fetch_and_persist_symbol(conn, sym, start_str, end_str)
            if n == 0:
                skipped.append(sym)
            else:
                print(f"{n} rows")
            total_rows += n

        print(f"\nDone. Inserted/updated {total_rows} OHLCV rows total.")
        if skipped:
            print(f"Skipped ({len(skipped)}): {', '.join(skipped)}")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
