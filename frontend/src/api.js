const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function request(path, { token, method = "GET", body } = {}) {
  const headers = {};

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  const contentType = response.headers.get("content-type") || "";
  const data = contentType.includes("application/json") ? await response.json() : null;

  if (!response.ok) {
    throw new ApiError(data?.message || "Erro na requisicao", response.status);
  }

  return data;
}

export function isUnauthorizedError(error) {
  return error instanceof ApiError && error.status === 401;
}

export const api = {
  register: (payload) => request("/auth/register", { method: "POST", body: payload }),
  login: (payload) => request("/auth/login", { method: "POST", body: payload }),
  refresh: (refreshToken) =>
    request("/auth/refresh", {
      method: "POST",
      token: refreshToken,
      body: { refreshToken },
    }),
  logout: (accessToken, refreshToken) =>
    request("/auth/logout", {
      method: "POST",
      token: accessToken,
      body: refreshToken ? { refreshToken } : {},
    }),
  listTasks: (token, status) => request(`/tasks?status=${status}`, { token }),
  createTask: (token, payload) => request("/tasks", { token, method: "POST", body: payload }),
  updateTask: (token, taskId, payload) => request(`/tasks/${taskId}`, { token, method: "PUT", body: payload }),
  deleteTask: (token, taskId) => request(`/tasks/${taskId}`, { token, method: "DELETE" }),
};
