import { useEffect, useMemo, useState } from "react";
import { api, isUnauthorizedError } from "./api";

const AUTH_STORAGE_KEY = "todo_auth";

function normalizeTask(raw) {
  return {
    id: raw.id,
    title: raw.title ?? "",
    description: raw.description ?? "",
    isCompleted: raw.isCompleted ?? raw.is_completed ?? false,
  };
}

function toAuthState(raw) {
  if (!raw || typeof raw !== "object") {
    return { accessToken: "", refreshToken: "", user: null };
  }

  return {
    accessToken: raw.accessToken ?? raw.token ?? "",
    refreshToken: raw.refreshToken ?? "",
    user: raw.user ?? null,
  };
}

function normalizeAuthResponse(data, fallbackUser) {
  const nextAuth = toAuthState(data);
  return {
    accessToken: nextAuth.accessToken,
    refreshToken: nextAuth.refreshToken,
    user: nextAuth.user ?? fallbackUser ?? null,
  };
}

export default function App() {
  const [authMode, setAuthMode] = useState("login");
  const [authForm, setAuthForm] = useState({ name: "", email: "", password: "" });
  const [auth, setAuth] = useState(() => {
    const saved = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!saved) {
      return { accessToken: "", refreshToken: "", user: null };
    }

    try {
      return toAuthState(JSON.parse(saved));
    } catch {
      return { accessToken: "", refreshToken: "", user: null };
    }
  });

  const [tasks, setTasks] = useState([]);
  const [filter, setFilter] = useState("all");
  const [taskForm, setTaskForm] = useState({ title: "", description: "" });
  const [editingId, setEditingId] = useState(null);
  const [editingForm, setEditingForm] = useState({ title: "", description: "" });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const isAuthenticated = Boolean(auth.accessToken);

  useEffect(() => {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
  }, [auth]);

  const clearSession = () => {
    setAuth({ accessToken: "", refreshToken: "", user: null });
    setTasks([]);
    setFilter("all");
    setEditingId(null);
  };

  async function executeWithAuth(operation) {
    if (!auth.accessToken) {
      throw new Error("usuario nao autenticado");
    }

    try {
      return await operation(auth.accessToken);
    } catch (error) {
      if (!isUnauthorizedError(error) || !auth.refreshToken) {
        throw error;
      }

      try {
        const refreshed = await api.refresh(auth.refreshToken);
        const nextAuth = normalizeAuthResponse(refreshed, auth.user);
        setAuth(nextAuth);
        return await operation(nextAuth.accessToken);
      } catch (refreshError) {
        clearSession();
        throw refreshError;
      }
    }
  }

  async function loadTasks() {
    if (!auth.accessToken) {
      setTasks([]);
      return;
    }

    setLoading(true);
    setError("");

    try {
      const data = await executeWithAuth((token) => api.listTasks(token, filter));
      setTasks((data || []).map(normalizeTask));
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadTasks();
  }, [auth.accessToken, auth.refreshToken, filter]);

  const stats = useMemo(() => {
    const total = tasks.length;
    const completed = tasks.filter((task) => task.isCompleted).length;
    const pending = total - completed;
    return { total, completed, pending };
  }, [tasks]);

  async function handleAuthSubmit(event) {
    event.preventDefault();
    setError("");

    try {
      const payload = {
        email: authForm.email.trim(),
        password: authForm.password,
      };

      const data = authMode === "register"
        ? await api.register({ ...payload, name: authForm.name.trim() })
        : await api.login(payload);

      setAuth(normalizeAuthResponse(data, null));
      setAuthForm({ name: "", email: "", password: "" });
    } catch (error) {
      setError(error.message);
    }
  }

  async function logout() {
    try {
      if (auth.accessToken) {
        await api.logout(auth.accessToken, auth.refreshToken || null);
      }
    } catch {
    } finally {
      clearSession();
    }
  }

  async function handleCreateTask(event) {
    event.preventDefault();
    setError("");

    try {
      await executeWithAuth((token) =>
        api.createTask(token, {
          title: taskForm.title,
          description: taskForm.description,
        })
      );

      setTaskForm({ title: "", description: "" });
      await loadTasks();
    } catch (error) {
      setError(error.message);
    }
  }

  function startEditing(task) {
    setEditingId(task.id);
    setEditingForm({
      title: task.title,
      description: task.description || "",
    });
  }

  async function saveEdit(taskId) {
    setError("");
    try {
      const updated = await executeWithAuth((token) =>
        api.updateTask(token, taskId, {
          title: editingForm.title,
          description: editingForm.description,
        })
      );

      setTasks((current) =>
        current.map((task) => (task.id === taskId ? normalizeTask(updated) : task))
      );
      setEditingId(null);
    } catch (error) {
      setError(error.message);
    }
  }

  async function toggleTask(task) {
    setError("");
    try {
      const updated = await executeWithAuth((token) =>
        api.updateTask(token, task.id, {
          isCompleted: !task.isCompleted,
        })
      );

      setTasks((current) =>
        current.map((item) => (item.id === task.id ? normalizeTask(updated) : item))
      );
    } catch (error) {
      setError(error.message);
    }
  }

  async function removeTask(taskId) {
    setError("");
    try {
      await executeWithAuth((token) => api.deleteTask(token, taskId));
      setTasks((current) => current.filter((task) => task.id !== taskId));
    } catch (error) {
      setError(error.message);
    }
  }

  return (
    <div className="app">
      <header className="header">
        <h1>To-Do List com JWT</h1>
        <p>Projeto full stack com React + Flask/ASP.NET Core</p>
      </header>

      {error ? <div className="error">{error}</div> : null}

      {!isAuthenticated ? (
        <section className="card auth-card">
          <div className="auth-switch">
            <button
              type="button"
              className={authMode === "login" ? "active" : ""}
              onClick={() => setAuthMode("login")}
            >
              Login
            </button>
            <button
              type="button"
              className={authMode === "register" ? "active" : ""}
              onClick={() => setAuthMode("register")}
            >
              Cadastro
            </button>
          </div>

          <form onSubmit={handleAuthSubmit} className="form">
            {authMode === "register" ? (
              <label>
                Nome
                <input
                  value={authForm.name}
                  onChange={(e) => setAuthForm((current) => ({ ...current, name: e.target.value }))}
                  required
                />
              </label>
            ) : null}

            <label>
              Email
              <input
                type="email"
                value={authForm.email}
                onChange={(e) => setAuthForm((current) => ({ ...current, email: e.target.value }))}
                required
              />
            </label>

            <label>
              Senha
              <input
                type="password"
                value={authForm.password}
                onChange={(e) => setAuthForm((current) => ({ ...current, password: e.target.value }))}
                required
              />
            </label>

            <button type="submit" className="primary">
              {authMode === "register" ? "Criar conta" : "Entrar"}
            </button>
          </form>
        </section>
      ) : (
        <>
          <section className="card toolbar">
            <div>
              <strong>{auth.user?.name}</strong>
              <span>{auth.user?.email}</span>
            </div>

            <div className="stats">
              <span>Total: {stats.total}</span>
              <span>Pendentes: {stats.pending}</span>
              <span>Concluidas: {stats.completed}</span>
            </div>

            <button type="button" onClick={logout} className="ghost">
              Sair
            </button>
          </section>

          <section className="card">
            <form onSubmit={handleCreateTask} className="form inline">
              <input
                placeholder="Nova tarefa"
                value={taskForm.title}
                onChange={(e) => setTaskForm((current) => ({ ...current, title: e.target.value }))}
                required
              />
              <input
                placeholder="Descricao (opcional)"
                value={taskForm.description}
                onChange={(e) => setTaskForm((current) => ({ ...current, description: e.target.value }))}
              />
              <button type="submit" className="primary">
                Adicionar
              </button>
            </form>
          </section>

          <section className="card">
            <div className="filters">
              {[
                { id: "all", label: "Todas" },
                { id: "pending", label: "Pendentes" },
                { id: "completed", label: "Concluidas" },
              ].map((item) => (
                <button
                  type="button"
                  key={item.id}
                  className={filter === item.id ? "active" : ""}
                  onClick={() => setFilter(item.id)}
                >
                  {item.label}
                </button>
              ))}
            </div>

            {loading ? <p>Carregando tarefas...</p> : null}

            <ul className="task-list">
              {tasks.map((task) => (
                <li key={task.id} className={task.isCompleted ? "done" : ""}>
                  <div className="task-main">
                    <input
                      type="checkbox"
                      checked={task.isCompleted}
                      onChange={() => toggleTask(task)}
                    />

                    {editingId === task.id ? (
                      <div className="edit-fields">
                        <input
                          value={editingForm.title}
                          onChange={(e) =>
                            setEditingForm((current) => ({ ...current, title: e.target.value }))
                          }
                        />
                        <input
                          value={editingForm.description}
                          onChange={(e) =>
                            setEditingForm((current) => ({ ...current, description: e.target.value }))
                          }
                        />
                      </div>
                    ) : (
                      <div>
                        <strong>{task.title}</strong>
                        {task.description ? <p>{task.description}</p> : null}
                      </div>
                    )}
                  </div>

                  <div className="task-actions">
                    {editingId === task.id ? (
                      <>
                        <button type="button" onClick={() => saveEdit(task.id)}>Salvar</button>
                        <button type="button" onClick={() => setEditingId(null)} className="ghost">
                          Cancelar
                        </button>
                      </>
                    ) : (
                      <button type="button" onClick={() => startEditing(task)}>Editar</button>
                    )}
                    <button type="button" onClick={() => removeTask(task.id)} className="danger">
                      Excluir
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          </section>
        </>
      )}
    </div>
  );
}
