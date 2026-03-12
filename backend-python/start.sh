set -e
python -m alembic -c alembic.ini upgrade head
if [ -z "$PORT" ]; then PORT=5000; fi
exec gunicorn --workers 2 --threads 4 --timeout 120 --bind 0.0.0.0:$PORT run:app
