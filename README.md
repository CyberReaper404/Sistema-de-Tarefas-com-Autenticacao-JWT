# Sistema de Tarefas com Autenticacao JWT

<div align="left">
  <img src="https://img.shields.io/badge/Python-3.12-blue" alt="Python 3.12" />
  <img src="https://img.shields.io/badge/Flask-3.0-green" alt="Flask 3.0" />
  <img src="https://img.shields.io/badge/C%23-.NET_8-purple" alt=".NET 8" />
  <img src="https://img.shields.io/badge/React-18-61DAFB" alt="React 18" />
  <img src="https://img.shields.io/badge/Auth-JWT-orange" alt="JWT" />
  <img src="https://img.shields.io/badge/PostgreSQL-Render-336791" alt="PostgreSQL Render" />
  <img src="https://img.shields.io/badge/Vercel-Frontend-black" alt="Vercel Frontend" />
  <img src="https://img.shields.io/badge/status-concluido-success" alt="Status concluido" />
  <img src="https://img.shields.io/badge/licenca-MIT-green" alt="Licenca MIT" />
</div>

[![CI](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/ci.yml)
[![Uptime Check](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/uptime.yml/badge.svg?branch=main)](https://github.com/CyberReaper404/Sistema-de-Tarefas-com-Autenticacao-JWT/actions/workflows/uptime.yml)

## Sobre o Projeto

Este projeto foi desenvolvido com o objetivo de praticar a construcao de uma aplicacao web completa, cobrindo front-end, back-end, banco de dados, autenticacao e deploy.

A proposta foi implementar o mesmo sistema de tarefas em duas stacks de backend diferentes, uma em Python com Flask e outra em C# com ASP.NET Core, mantendo a mesma ideia de negocio e a mesma experiencia no frontend.

Links publicados:

- Frontend: `https://sistema-de-tarefas-com-autenticacao.vercel.app/`
- API: `https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api/health`

## Funcionalidades

- Cadastro e autenticacao de usuarios com JWT
- Access token e refresh token
- CRUD completo de tarefas
- Marcacao de tarefas como concluidas ou pendentes
- Filtros por status: `all`, `pending`, `completed`
- Isolamento por usuario: cada conta acessa apenas as proprias tarefas
- Duas implementacoes backend: Flask e ASP.NET Core
- Frontend integrado com API publicada em producao

## Arquitetura do Projeto

```text
/
|-- frontend/               # Aplicacao React + Vite
|-- backend-python/         # API em Flask com JWT, SQLAlchemy e Alembic
|-- backend-dotnet/         # API em ASP.NET Core com JWT e EF Core
|-- backend-dotnet-tests/   # Testes automatizados do backend C#
|-- scripts/                # Scripts para execucao local
|-- render.yaml             # Configuracao de deploy da API Python
`-- .github/workflows/      # CI e monitoramento
```

## Tecnologias Utilizadas

### Frontend

- React
- Vite
- Vitest
- Testing Library

### Backend Python

- Flask
- Flask-SQLAlchemy
- Flask-JWT-Extended
- Alembic
- Gunicorn

### Backend C#

- ASP.NET Core Web API
- Entity Framework Core
- JWT Bearer Authentication
- xUnit

### Banco de Dados

- SQLite para desenvolvimento local
- PostgreSQL para producao

## Como Executar

### Execucao Rapida

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-python-stack.ps1
```

Ambiente local:

- Frontend: `http://localhost:5173`
- API Python: `http://localhost:5000`

Para encerrar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\stop-python-stack.ps1
```

### Execucao Manual

#### Backend Python

```powershell
cd backend-python
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
$env:DATABASE_URL="sqlite:///$((Join-Path (Get-Location) 'todo.db') -replace '\\','/')"
python -m alembic -c alembic.ini upgrade head
python run.py
```

#### Backend C#

```powershell
cd backend-dotnet
dotnet restore
dotnet run
```

#### Frontend

```powershell
cd frontend
npm install
copy /Y .env.example .env
npm run dev
```

## Deploy

### Frontend no Vercel

Configuracao utilizada:

- Root Directory: `frontend`
- Variavel de ambiente:

```text
VITE_API_URL=https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api
```

URL publicada:

- `https://sistema-de-tarefas-com-autenticacao.vercel.app/`

### Backend Python no Render

Configuracao utilizada:

- Root Directory: `backend-python`
- Build Command:

```text
pip install -r requirements.txt
```

- Start Command:

```text
bash start.sh
```

- Variaveis principais:

```text
DATABASE_URL=<postgres-url>
SECRET_KEY=<secret>
JWT_SECRET_KEY=<secret>
JWT_ACCESS_TOKEN_MINUTES=30
JWT_REFRESH_TOKEN_DAYS=7
CORS_ALLOWED_ORIGINS=https://sistema-de-tarefas-com-autenticacao.vercel.app
```

Health check:

- `https://sistema-de-tarefas-com-autenticacao-jwt.onrender.com/api/health`

## Endpoints da API

### Autenticacao

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

### Tarefas

- `GET /api/tasks?status=all`
- `GET /api/tasks?status=pending`
- `GET /api/tasks?status=completed`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`

## Testes

### Python

```powershell
cd backend-python
.\.venv\Scripts\python.exe smoke_test.py
```

### C#

```powershell
cd backend-dotnet-tests
dotnet test
```

### Frontend

```powershell
cd frontend
npm run test:run
npm run build
```

## GitHub Actions

Este projeto possui automacoes configuradas para:

- Rodar testes e build automaticamente a cada `push` ou `pull request`
- Verificar periodicamente se frontend e API continuam online

Para o monitoramento funcionar no GitHub, configure as variables do repositorio:

```text
API_HEALTH_URL=https://todo-api-python-3nxj.onrender.com/api/health
FRONTEND_URL=https://sistema-de-tarefas-com-autenticacao.vercel.app/
```

## Objetivo do Projeto

Este projeto foi pensado para demonstrar pratica em:

- Criacao de APIs REST
- Autenticacao com JWT
- Integracao entre frontend e backend
- Persistencia em banco relacional
- Testes automatizados
- CI com GitHub Actions
- Deploy em producao

## Licenca

Este projeto esta sob a licenca MIT. Consulte o arquivo `LICENSE` para mais informacoes.
