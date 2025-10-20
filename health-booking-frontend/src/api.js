// src/api.js
const API_BASE =
  (import.meta?.env?.VITE_API_BASE) ||
  (process.env.REACT_APP_API_BASE) ||
  "http://localhost:5102";

async function jsonFetch(path, { method = "GET", body, token } = {}) {
  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) {
    const msg = typeof data === "string" ? data : (data?.message || data?.error || res.statusText);
    throw new Error(msg || `HTTP ${res.status}`);
  }
  return data;
}

export const api = {
  // health
  health: () => jsonFetch("/api/auth/health"),

  // auth
  signup: ({ name, email, password, role = "patient" }) =>
    jsonFetch("/api/auth/signup", { method: "POST", body: { name, email, password, role } }),

  login: ({ email, password }) =>
    jsonFetch("/api/auth/login", { method: "POST", body: { email, password } }),

  me: (token) => jsonFetch("/api/auth/me", { token }),

  refresh: (refreshToken) =>
    jsonFetch("/api/auth/refresh", { method: "POST", body: { refreshToken } }),

  logout: (refreshToken) =>
    jsonFetch("/api/auth/logout", { method: "POST", body: { refreshToken } }),

  // data
  doctors: () => jsonFetch("/api/doctors"),
  appointments: (token) => jsonFetch("/api/appointments", { token }), // your backend doesn't require auth here, but it's fine to pass token
  createAppointment: (token, { patientName, doctorId, time }) =>
    jsonFetch("/api/appointments", { method: "POST", token, body: { patientName, doctorId, time } }),
};

export { API_BASE };
