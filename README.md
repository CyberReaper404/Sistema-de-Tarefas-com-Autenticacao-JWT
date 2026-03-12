# Sistema de Tarefas com Autenticacao JWT

[![CI](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/ci.yml)
[![Uptime Check](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/uptime.yml/badge.svg?branch=main)](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/uptime.yml)

<p align="left">
  <img src="https://img.shields.io/badge/Python-3.12%2B-blue" alt="Python 3.12+" />
  <img src="https://img.shields.io/badge/Flask-3.0-green" alt="Flask 3.0" />
  <img src="https://img.shields.io/badge/C%23-.NET_8-purple" alt="C# .NET 8" />
  <img src="https://img.shields.io/badge/React-18-61DAFB" alt="React 18" />
  <img src="https://img.shields.io/badge/Auth-JWT%20Access%2FRefresh-orange" alt="JWT" />
  <img src="https://img.shields.io/badge/DB-SQLite%20%7C%20PostgreSQL-4479A1" alt="SQLite | PostgreSQL" />
  <img src="https://img.shields.io/badge/licenca-MIT-green" alt="Licenca MIT" />
</p>

## Visao Geral

Este projeto e uma aplicacao full stack de gerenciamento de tarefas com autenticacao JWT, isolamento por usuario e fluxo completo de criacao, edicao e organizacao de tarefas.

A proposta foi desenvolver a mesma solucao em duas stacks de backend para demonstrar dominio de conceitos comuns em projetos reais, como autenticacao, persistencia, organizacao por camadas, validacao automatizada e deploy.

Projeto publicado:
- Frontend: `https://cyberreaper404-todo-auth.vercel.app/`
- API: `https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api/health`

## O que a aplicacao faz

- Cadastro e login de usuarios
- Autenticacao com access token e refresh token
- CRUD completo de tarefas
- Marcacao de tarefas como concluidas ou pendentes
- Filtro por status: `all`, `pending`, `completed`
- Isolamento de dados por usuario autenticado

## Stack utilizada

- Frontend: React + Vite
- Backend Python: Flask + SQLAlchemy + Alembic
- Backend C#: ASP.NET Core + Entity Framework Core
- Banco de dados: SQLite para desenvolvimento local e PostgreSQL para deploy
- Autenticacao: JWT

## Estrutura do repositorio

```text
/
|-- frontend/
|-- backend-python/
|-- backend-dotnet/
|-- backend-dotnet-tests/
|-- scripts/
|-- render.yaml
`-- .github/workflows/
Como rodar localmente
Opcao rapida
powershell -ExecutionPolicy Bypass -File .\scripts\start-python-stack.ps1
Ambiente local:

Frontend: http://localhost:5173
API Python: http://localhost:5000
Para parar:

powershell -ExecutionPolicy Bypass -File .\scripts\stop-python-stack.ps1
Opcao manual
Backend Python:

cd backend-python
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
$env:DATABASE_URL="sqlite:///$((Join-Path (Get-Location) 'todo.db') -replace '\\','/')"
python -m alembic -c alembic.ini upgrade head
python run.py
Backend C#:

cd backend-dotnet
dotnet restore
dotnet run
Frontend:

cd frontend
npm install
copy /Y .env.example .env
npm run dev
Deploy
Backend no Render
Configuracao usada no deploy:

Root Directory: backend-python
Build Command: pip install -r requirements.txt
Start Command: bash start.sh
Variaveis de ambiente principais:

DATABASE_URL
SECRET_KEY
JWT_SECRET_KEY
JWT_ACCESS_TOKEN_MINUTES=30
JWT_REFRESH_TOKEN_DAYS=7
CORS_ALLOWED_ORIGINS=https://cyberreaper404-todo-auth.vercel.app
Health check:

https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api/health
Frontend no Vercel
Configuracao usada no deploy:

Root Directory: frontend
Variavel de ambiente:

VITE_API_URL=https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api
Projeto publicado:

https://cyberreaper404-todo-auth.vercel.app/
Testes e automacao
Este repositorio possui GitHub Actions para duas finalidades:

CI: roda automaticamente testes e build do frontend, smoke test da API Python e testes da API C# a cada push ou pull request
Uptime Check: faz verificacoes agendadas do frontend e da API publicados
Para o monitoramento funcionar no GitHub Actions, configure em Settings > Secrets and variables > Actions > Variables:

API_HEALTH_URL=https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api/health
FRONTEND_URL=https://cyberreaper404-todo-auth.vercel.app/
Comandos de teste
Python:

cd backend-python
.\.venv\Scripts\python.exe smoke_test.py
Frontend:

cd frontend
npm run test:run
npm run build
C#:

cd backend-dotnet-tests
dotnet test
Endpoints principais
Auth:

POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
Tasks:

GET /api/tasks?status=all|pending|completed
POST /api/tasks
PUT /api/tasks/{id}
DELETE /api/tasks/{id}
Licenca
Distribuido sob a licenca MIT. Veja LICENSE.

