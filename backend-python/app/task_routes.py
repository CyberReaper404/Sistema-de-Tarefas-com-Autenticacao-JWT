from flask import Blueprint, request
from flask_jwt_extended import get_jwt_identity, jwt_required

from .extensions import db
from .models import Task

task_bp = Blueprint("tasks", __name__)


def _query_for_user(user_id: int):
    return Task.query.filter_by(user_id=user_id)


@task_bp.get("")
@jwt_required()
def list_tasks():
    user_id = int(get_jwt_identity())
    status = (request.args.get("status") or "all").lower()

    query = _query_for_user(user_id)
    if status == "pending":
        query = query.filter_by(is_completed=False)
    elif status == "completed":
        query = query.filter_by(is_completed=True)

    tasks = query.order_by(Task.created_at.desc()).all()
    return [task.to_dict() for task in tasks], 200


@task_bp.post("")
@jwt_required()
def create_task():
    user_id = int(get_jwt_identity())
    payload = request.get_json(silent=True) or {}

    title = (payload.get("title") or "").strip()
    description = (payload.get("description") or "").strip() or None

    if not title:
        return {"message": "title e obrigatorio"}, 400

    task = Task(title=title, description=description, user_id=user_id)
    db.session.add(task)
    db.session.commit()

    return task.to_dict(), 201


@task_bp.put("/<int:task_id>")
@jwt_required()
def update_task(task_id: int):
    user_id = int(get_jwt_identity())
    payload = request.get_json(silent=True) or {}

    task = _query_for_user(user_id).filter_by(id=task_id).first()
    if not task:
        return {"message": "tarefa nao encontrada"}, 404

    if "title" in payload:
        new_title = (payload.get("title") or "").strip()
        if not new_title:
            return {"message": "title nao pode ficar vazio"}, 400
        task.title = new_title

    if "description" in payload:
        desc = payload.get("description")
        task.description = desc.strip() if isinstance(desc, str) else None

    if "isCompleted" in payload:
        task.is_completed = bool(payload.get("isCompleted"))

    db.session.commit()
    return task.to_dict(), 200


@task_bp.delete("/<int:task_id>")
@jwt_required()
def delete_task(task_id: int):
    user_id = int(get_jwt_identity())
    task = _query_for_user(user_id).filter_by(id=task_id).first()

    if not task:
        return {"message": "tarefa nao encontrada"}, 404

    db.session.delete(task)
    db.session.commit()
    return {"message": "tarefa removida"}, 200