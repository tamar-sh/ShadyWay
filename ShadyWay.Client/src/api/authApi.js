const API_BASE = 'https://localhost:7168';

export async function register(identityCard, fullName, email, password, shadowPreference) {
  const response = await fetch(`${API_BASE}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ identityCard, fullName, email, password, shadowPreference }),
  });
  if (!response.ok) {
    const msg = await response.text();
    throw new Error(msg);
  }
  return response.json();
}

export async function login(email, password) {
  const response = await fetch(`${API_BASE}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!response.ok) {
    const msg = await response.text();
    throw new Error(msg);
  }
  return response.json();
}
