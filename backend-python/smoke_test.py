import os
from pathlib import Path

root = Path(__file__).resolve().parent
smoke_db = root / "smoke.db"
os.environ["DATABASE_URL"] = f"sqlite:///{smoke_db.as_posix()}"
os.environ["JWT_ACCESS_TOKEN_MINUTES"] = "30"
os.environ["JWT_REFRESH_TOKEN_DAYS"] = "7"

if smoke_db.exists():
    smoke_db.unlink()

from app import create_app
from app.extensions import db

app = create_app()

with app.app_context():
    db.create_all()

client = app.test_client()

register_response = client.post(
    "/api/auth/register",
    json={"name": "Maria", "email": "maria@example.com", "password": "123456"},
)
assert register_response.status_code == 201, register_response.get_data(as_text=True)
register_payload = register_response.get_json()
access_token = register_payload["accessToken"]
refresh_token = register_payload["refreshToken"]

task_create = client.post(
    "/api/tasks",
    headers={"Authorization": f"Bearer {access_token}"},
    json={"title": "Estudar JWT", "description": "Ler docs"},
)
assert task_create.status_code == 201, task_create.get_data(as_text=True)
created_task = task_create.get_json()

task_filter_pending = client.get(
    "/api/tasks?status=pending",
    headers={"Authorization": f"Bearer {access_token}"},
)
assert task_filter_pending.status_code == 200
assert len(task_filter_pending.get_json()) == 1

task_complete = client.put(
    f"/api/tasks/{created_task['id']}",
    headers={"Authorization": f"Bearer {access_token}"},
    json={"isCompleted": True},
)
assert task_complete.status_code == 200

task_filter_completed = client.get(
    "/api/tasks?status=completed",
    headers={"Authorization": f"Bearer {access_token}"},
)
assert task_filter_completed.status_code == 200
assert len(task_filter_completed.get_json()) == 1

refresh_response = client.post(
    "/api/auth/refresh",
    headers={"Authorization": f"Bearer {refresh_token}"},
)
assert refresh_response.status_code == 200, refresh_response.get_data(as_text=True)
refresh_payload = refresh_response.get_json()
new_access_token = refresh_payload["accessToken"]
new_refresh_token = refresh_payload["refreshToken"]

refresh_reuse_response = client.post(
    "/api/auth/refresh",
    headers={"Authorization": f"Bearer {refresh_token}"},
)
assert refresh_reuse_response.status_code == 401

logout_response = client.post(
    "/api/auth/logout",
    headers={"Authorization": f"Bearer {new_access_token}"},
    json={"refreshToken": new_refresh_token},
)
assert logout_response.status_code == 200

token_after_logout = client.get(
    "/api/tasks",
    headers={"Authorization": f"Bearer {new_access_token}"},
)
assert token_after_logout.status_code == 401

second_user_register = client.post(
    "/api/auth/register",
    json={"name": "Ana", "email": "ana@example.com", "password": "abcdef"},
)
assert second_user_register.status_code == 201
second_payload = second_user_register.get_json()
second_access = second_payload["accessToken"]

second_user_tasks = client.get(
    "/api/tasks",
    headers={"Authorization": f"Bearer {second_access}"},
)
assert second_user_tasks.status_code == 200
assert len(second_user_tasks.get_json()) == 0

unauthorized_update = client.put(
    f"/api/tasks/{created_task['id']}",
    headers={"Authorization": f"Bearer {second_access}"},
    json={"title": "Hack"},
)
assert unauthorized_update.status_code == 404

print("SMOKE TEST PASS")
