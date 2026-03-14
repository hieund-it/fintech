"""Application configuration loaded from environment variables."""
from urllib.parse import quote_plus

from pydantic import computed_field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"

    # Individual Postgres params — avoids URL-encoding issues with special chars in passwords
    postgres_host: str = "localhost"
    postgres_port: int = 5432
    postgres_db: str = "vnstock"
    postgres_user: str = "postgres"
    postgres_password: str = "changeme"

    # Polling interval in seconds between vnstock API calls
    tcbs_poll_interval: float = 3.0
    # How often to flush tick buffer to PostgreSQL (seconds)
    batch_persist_interval: float = 5.0
    symbols_file: str = "symbols.json"
    log_level: str = "INFO"

    @computed_field
    @property
    def postgres_dsn(self) -> str:
        """Build asyncpg DSN with URL-encoded credentials (handles special chars like @)."""
        user = quote_plus(self.postgres_user)
        pwd = quote_plus(self.postgres_password)
        return f"postgresql://{user}:{pwd}@{self.postgres_host}:{self.postgres_port}/{self.postgres_db}"

    class Config:
        env_file = ".env"


settings = Settings()
