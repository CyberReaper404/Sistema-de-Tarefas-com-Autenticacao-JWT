import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { vi } from "vitest";
import App from "./App";

function mockJsonResponse(status, body) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    headers: {
      get: () => "application/json",
    },
    json: async () => body,
  });
}

describe("App", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("alterna para modo cadastro", async () => {
    render(<App />);

    await userEvent.click(screen.getByRole("button", { name: "Cadastro" }));

    expect(screen.getByLabelText("Nome")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Criar conta" })).toBeInTheDocument();
  });

  it("faz login e carrega tarefas", async () => {
    const fetchMock = vi.spyOn(global, "fetch");
    fetchMock
      .mockImplementationOnce(() =>
        mockJsonResponse(200, {
          accessToken: "access-token",
          refreshToken: "refresh-token",
          user: { id: 1, name: "Maria", email: "maria@example.com" },
        })
      )
      .mockImplementationOnce(() => mockJsonResponse(200, []));

    render(<App />);

    await userEvent.type(screen.getByLabelText("Email"), "maria@example.com");
    await userEvent.type(screen.getByLabelText("Senha"), "123456");
    await userEvent.click(screen.getByRole("button", { name: "Entrar" }));

    await waitFor(() => {
      expect(screen.getByText("maria@example.com")).toBeInTheDocument();
    });

    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("renova token automaticamente quando recebe 401", async () => {
    localStorage.setItem(
      "todo_auth",
      JSON.stringify({
        accessToken: "expired-access",
        refreshToken: "refresh-token",
        user: { id: 1, name: "Maria", email: "maria@example.com" },
      })
    );

    const fetchMock = vi.spyOn(global, "fetch");
    fetchMock
      .mockImplementationOnce(() => mockJsonResponse(401, { message: "token expirado" }))
      .mockImplementationOnce(() =>
        mockJsonResponse(200, {
          accessToken: "new-access",
          refreshToken: "new-refresh",
          user: { id: 1, name: "Maria", email: "maria@example.com" },
        })
      )
      .mockImplementationOnce(() => mockJsonResponse(200, []))
      .mockImplementation(() => mockJsonResponse(200, []));

    render(<App />);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThanOrEqual(4);
    });

    const savedAuth = JSON.parse(localStorage.getItem("todo_auth"));
    expect(savedAuth.accessToken).toBe("new-access");
    expect(savedAuth.refreshToken).toBe("new-refresh");
  });
});
