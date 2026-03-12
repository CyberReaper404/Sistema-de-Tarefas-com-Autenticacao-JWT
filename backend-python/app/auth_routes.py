from datetime import datetime, timezone

from flask import Blueprint, request
from flask_jwt_extended import (
    create_access_token,
    create_refresh_token,
    decode_token,
    get_jwt,
    get_jwt_identity,
    jwt_required,
)

from .extensions import bcrypt, db
from .models import RefreshToken, TokenBlocklist, User

auth_bp = Blueprint("auth", __name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _to_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)


def _decode_refresh(refresh_token: str) -> tuple[str, datetime]:
    payload = decode_token(refresh_token)
    token_jti = payload.get("jti")
    expires_unix = payload.get("exp")
    if not isinstance(token_jti, str) or not isinstance(expires_unix, int):
        raise ValueError("refresh token invalido")
    return token_jti, datetime.fromtimestamp(expires_unix, timezone.utc)


def _create_token_pair(user: User) -> tuple[str, str, RefreshToken]:
    access_token = create_access_token(identity=str(user.id))
    refresh_token = create_refresh_token(identity=str(user.id))
    refresh_jti, refresh_expires_at = _decode_refresh(refresh_token)

    refresh_record = RefreshToken(
        token_jti=refresh_jti,
        user_id=user.id,
        expires_at=refresh_expires_at,
    )

    return access_token, refresh_token, refresh_record


def _auth_payload(user: User, access_token: str, refresh_token: str) -> dict:
    return {
        "token": access_token,
        "accessToken": access_token,
        "refreshToken": refresh_token,
        "user": user.to_dict(),
    }


@auth_bp.post("/register")
def register():
    payload = request.get_json(silent=True) or {}
    name = (payload.get("name") or "").strip()
    email = (payload.get("email") or "").strip().lower()
    password = payload.get("password") or ""

    if not name or not email or not password:
        return {"message": "name, email e password sao obrigatorios"}, 400

    existing = User.query.filter_by(email=email).first()
    if existing:
        return {"message": "email ja cadastrado"}, 409

    password_hash = bcrypt.generate_password_hash(password).decode("utf-8")
    user = User(name=name, email=email, password_hash=password_hash)
    db.session.add(user)
    db.session.flush()

    access_token, refresh_token, refresh_record = _create_token_pair(user)
    db.session.add(refresh_record)
    db.session.commit()

    return _auth_payload(user, access_token, refresh_token), 201


@auth_bp.post("/login")
def login():
    payload = request.get_json(silent=True) or {}
    email = (payload.get("email") or "").strip().lower()
    password = payload.get("password") or ""

    if not email or not password:
        return {"message": "email e password sao obrigatorios"}, 400

    user = User.query.filter_by(email=email).first()
    if not user or not bcrypt.check_password_hash(user.password_hash, password):
        return {"message": "credenciais invalidas"}, 401

    access_token, refresh_token, refresh_record = _create_token_pair(user)
    db.session.add(refresh_record)
    db.session.commit()

    return _auth_payload(user, access_token, refresh_token), 200


@auth_bp.post("/refresh")
@jwt_required(refresh=True)
def refresh():
    user_id = int(get_jwt_identity())
    token_jti = get_jwt().get("jti")

    if not isinstance(token_jti, str):
        return {"message": "refresh token invalido"}, 401

    refresh_record = RefreshToken.query.filter_by(token_jti=token_jti, user_id=user_id).first()
    if not refresh_record or refresh_record.revoked_at is not None:
        return {"message": "refresh token invalido"}, 401

    if _to_utc(refresh_record.expires_at) <= _utc_now():
        return {"message": "refresh token expirado"}, 401

    user = User.query.get(user_id)
    if not user:
        return {"message": "usuario nao encontrado"}, 404

    refresh_record.revoked_at = _utc_now()

    access_token, new_refresh_token, new_refresh_record = _create_token_pair(user)
    db.session.add(new_refresh_record)
    db.session.commit()

    return _auth_payload(user, access_token, new_refresh_token), 200


@auth_bp.post("/logout")
@jwt_required()
def logout():
    user_id = int(get_jwt_identity())
    token_jti = get_jwt().get("jti")
    payload = request.get_json(silent=True) or {}

    if isinstance(token_jti, str):
        exists = TokenBlocklist.query.filter_by(token_jti=token_jti).first()
        if not exists:
            db.session.add(TokenBlocklist(token_jti=token_jti, token_type="access", user_id=user_id))

    refresh_token = payload.get("refreshToken")
    if isinstance(refresh_token, str) and refresh_token.strip():
        try:
            refresh_jti, _ = _decode_refresh(refresh_token.strip())
            refresh_record = RefreshToken.query.filter_by(token_jti=refresh_jti, user_id=user_id).first()
            if refresh_record and refresh_record.revoked_at is None:
                refresh_record.revoked_at = _utc_now()
        except Exception:
            pass

    db.session.commit()
    return {"message": "logout realizado"}, 200
