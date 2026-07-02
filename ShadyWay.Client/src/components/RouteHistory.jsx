import { useState, useEffect } from 'react';

async function reverseGeocode(lat, lon) {
  const key   = `${lat.toFixed(6)},${lon.toFixed(6)}`;
  const cache = JSON.parse(localStorage.getItem('shadyway_addr_cache') || '{}');
  if (cache[key]) return cache[key];
  try {
    const res  = await fetch(
      `https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lon}&format=json&accept-language=he`,
      { headers: { 'Accept-Language': 'he' } }
    );
    const data = await res.json();
    const name = data.address?.road || data.address?.neighbourhood || data.display_name?.split(',')[0] || key;
    cache[key] = name;
    localStorage.setItem('shadyway_addr_cache', JSON.stringify(cache));
    return name;
  } catch { return key; }
}

export default function RouteHistory({ history, onSelect }) {
  const [names, setNames] = useState({});

  useEffect(() => {
    if (!history?.length) return;
    const coords = [...new Set(
      history.flatMap(h => [h.sourceAddress, h.destAddress])
    )];
    coords.forEach(async (addr) => {
      const [lat, lon] = addr.split(',').map(Number);
      const name = await reverseGeocode(lat, lon);
      setNames(prev => ({ ...prev, [addr]: name }));
    });
  }, [history]);

  function getLabel(addr) {
    return names[addr] || formatAddress(addr);
  }

  if (!history || history.length === 0) {
    return <p style={styles.empty}>אין מסלולים קודמים עדיין.</p>;
  }

  return (
    <div style={styles.list}>
      {history.map((item) => (
        <div key={item.requestId} style={styles.card}>
          <div style={styles.addresses}>
            <span style={styles.label}>מוצא: {getLabel(item.sourceAddress)}</span>
            <span style={styles.arrow}>↓</span>
            <span style={styles.label}>יעד: {getLabel(item.destAddress)}</span>
          </div>
          <div style={styles.meta}>
            <span>{(item.totalDistanceMeters / 1000).toFixed(2)} ק"מ</span>
            <span>{item.shadowPercentage.toFixed(0)}% צל</span>
          </div>
          <div style={styles.time}>{formatDate(item.requestTime)}</div>
          <button style={styles.btn} onClick={() => onSelect(item)}>
            חשב מחדש עם השמש הנוכחית ←
          </button>
        </div>
      ))}
    </div>
  );
}

function formatAddress(coordStr) {
  const cache = JSON.parse(localStorage.getItem('shadyway_addr_cache') || '{}');
  const [lat, lon] = coordStr.split(',').map(Number);
  const key = `${lat.toFixed(6)},${lon.toFixed(6)}`;
  return cache[key] || `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
}

function formatDate(isoString) {
  const d = new Date(isoString);
  return d.toLocaleString('he-IL', {
    day: '2-digit', month: '2-digit', year: '2-digit',
    hour: '2-digit', minute: '2-digit',
  });
}

const styles = {
  list:  { display: 'flex', flexDirection: 'column', gap: 8 },
  empty: { fontSize: 12, color: '#9ca3af', textAlign: 'center' },
  card:  {
    background: '#f8fafc',
    border: '1px solid #e2e8f0',
    borderRadius: 8,
    padding: '10px 12px',
    display: 'flex',
    flexDirection: 'column',
    gap: 4,
    fontSize: 12,
  },
  addresses: { display: 'flex', flexDirection: 'column', gap: 2 },
  label: { color: '#374151' },
  arrow: { color: '#9ca3af', fontSize: 10, paddingRight: 4 },
  meta:  { display: 'flex', gap: 10, color: '#6b7280' },
  time:  { color: '#9ca3af', fontSize: 11 },
  btn: {
    marginTop: 4,
    padding: '6px 10px',
    background: '#15803d',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
    fontSize: 12,
    textAlign: 'right',
  },
};
