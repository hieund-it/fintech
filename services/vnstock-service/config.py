"""Application configuration loaded from environment variables."""
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"
    postgres_dsn: str = "postgresql://vnstock_user:changeme@localhost:5432/vnstock"
    # Polling interval in seconds between vnstock API calls
    tcbs_poll_interval: float = 3.0
    # How often to flush tick buffer to PostgreSQL (seconds)
    batch_persist_interval: float = 5.0
    symbols_file: str = "symbols.json"
    log_level: str = "INFO"

    class Config:
        env_file = ".env"


settings = Settings()
