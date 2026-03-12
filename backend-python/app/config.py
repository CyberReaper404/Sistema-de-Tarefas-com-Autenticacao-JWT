import os
from datetime import timedelta


def normalize_database_url(value: str | None) -> str:
    if not value:
        return "sqlite:///todo.db"
    if value.startswith("postgresql+psycopg://"):
        return value
    if value.startswith("postgres://"):
        return value.replace("postgres://", "postgresql+psycopg://", 1)
    if value.startswith("postgresql://"):
        return value.replace("postgresql://", "postgresql+psycopg://", 1)
    return value


def _to_int(value: str | None, default: int) -> int:
    if value is None:
        return default
    try:
        parsed = int(value)
        return parsed if parsed > 0 else default
    except ValueError:
        return default


def _to_origins(value: str | None) -> list[str]:
    if not value:
        return ["http://localhost:5173"]
    origins = [item.strip() for item in value.split(",") if item.strip()]
    return origins or ["http://localhost:5173"]


class Config:
    SECRET_KEY = os.getenv("SECRET_KEY", "dev-secret-change-this-in-production")
    JWT_SECRET_KEY = os.getenv(
        "JWT_SECRET_KEY",
        "dev-jwt-secret-change-this-in-production-32chars-minimum",
    )
    SQLALCHEMY_DATABASE_URI = normalize_database_url(os.getenv("DATABASE_URL"))
    SQLALCHEMY_TRACK_MODIFICATIONS = False
    JWT_ACCESS_TOKEN_EXPIRES = timedelta(minutes=_to_int(os.getenv("JWT_ACCESS_TOKEN_MINUTES"), 30))
    JWT_REFRESH_TOKEN_EXPIRES = timedelta(days=_to_int(os.getenv("JWT_REFRESH_TOKEN_DAYS"), 7))
    CORS_ALLOWED_ORIGINS = _to_origins(os.getenv("CORS_ALLOWED_ORIGINS"))
