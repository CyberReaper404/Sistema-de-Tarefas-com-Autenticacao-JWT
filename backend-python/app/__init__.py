from datetime import datetime, timezone

from flask import Flask
from flask_cors import CORS

from .auth_routes import auth_bp
from .config import Config
from .extensions import bcrypt, db, jwt
from .models import RefreshToken, TokenBlocklist
from .task_routes import task_bp


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _to_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)


def create_app():
    app = Flask(__name__)
    app.config.from_object(Config)

    db.init_app(app)
    jwt.init_app(app)
    bcrypt.init_app(app)
    CORS(app, resources={r"/api/*": {"origins": app.config["CORS_ALLOWED_ORIGINS"]}})

    app.register_blueprint(auth_bp, url_prefix="/api/auth")
    app.register_blueprint(task_bp, url_prefix="/api/tasks")

    @jwt.token_in_blocklist_loader
    def is_token_revoked(jwt_header, jwt_payload):
        token_jti = jwt_payload.get("jti")
        token_type = jwt_payload.get("type")

        if not isinstance(token_jti, str):
            return True

        revoked = TokenBlocklist.query.filter_by(token_jti=token_jti).first()
        if revoked is not None:
            return True

        if token_type == "refresh":
            refresh_record = RefreshToken.query.filter_by(token_jti=token_jti).first()
            if refresh_record is None:
                return True
            if refresh_record.revoked_at is not None:
                return True
            if _to_utc(refresh_record.expires_at) <= _utc_now():
                return True

        return False

    @jwt.revoked_token_loader
    def revoked_token_callback(jwt_header, jwt_payload):
        return {"message": "token revogado"}, 401

    @jwt.invalid_token_loader
    def invalid_token_callback(error):
        return {"message": "token invalido"}, 401

    @jwt.expired_token_loader
    def expired_token_callback(jwt_header, jwt_payload):
        return {"message": "token expirado"}, 401

    @app.get("/api/health")
    def health_check():
        return {"status": "ok"}, 200

    return app
