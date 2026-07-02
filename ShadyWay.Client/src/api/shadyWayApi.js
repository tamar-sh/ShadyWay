const API_BASE = 'https://localhost:7168';

export async function getRouteHistory(token) {
  const response = await fetch(`${API_BASE}/api/history`, {
    headers: { 'Authorization': `Bearer ${token}` },
  });
  if (!response.ok) throw new Error(`שגיאת שרת: ${response.status}`);
  return response.json();
}

export async function calculateRoute(startLat, startLon, endLat, endLon, shadowPreference, utcDateTime, token) {
  const response = await fetch(`${API_BASE}/api/route`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
    },
    body: JSON.stringify({
      startLatitude: startLat,
      startLongitude: startLon,
      endLatitude: endLat,
      endLongitude: endLon,
      shadowPreference,
      utcDateTime: utcDateTime ?? new Date().toISOString(),
    }),
  });

  if (!response.ok) {
    throw new Error(`שגיאת שרת: ${response.status}`);
  }

  return response.json();
}
