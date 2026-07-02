import { useState, useEffect, useRef } from 'react';

const SAVED_KEY = 'shadyway_saved_addresses';

function getSaved() {
  try { return JSON.parse(localStorage.getItem(SAVED_KEY) || '[]'); } catch { return []; }
}
function saveAddress(name, coords) {
  const saved = getSaved().filter(a => a.name !== name);
  localStorage.setItem(SAVED_KEY, JSON.stringify([{ name, coords }, ...saved].slice(0, 10)));
}

export default function AddressSearch({ placeholder, onSelect }) {
  const [query, setQuery]       = useState('');
  const [results, setResults]   = useState([]);
  const [open, setOpen]         = useState(false);
  const [loading, setLoading]   = useState(false);
  const [saved, setSaved]       = useState(getSaved);
  const [showSaved, setShowSaved] = useState(false);
  const [selected, setSelected] = useState(null); // { name, coords }
  const timerRef                = useRef(null);

  useEffect(() => {
    if (query.trim().length < 2) { setResults([]); setOpen(false); return; }

    const controller = new AbortController();
    clearTimeout(timerRef.current);
    timerRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        const url =
          `https://nominatim.openstreetmap.org/search` +
          `?q=${encodeURIComponent(query)}` +
          `&format=json&countrycodes=il&limit=6&accept-language=he`;
        const res  = await fetch(url, { headers: { 'Accept-Language': 'he' }, signal: controller.signal });
        const data = await res.json();
        setResults(data);
        setOpen(data.length > 0);
      } catch (err) {
        if (err.name !== 'AbortError') setResults([]);
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }, 350);

    // מבטל בקשה קודמת שעדיין באוויר כשהמשתמש מקליד תו נוסף,
    // כדי שתשובה ישנה ומאוחרת לא "תדרוס" תוצאות עדכניות
    return () => {
      clearTimeout(timerRef.current);
      controller.abort();
    };
  }, [query]);

  function handleSelect(item) {
    const name   = item.display_name.split(',')[0];
    const coords = [parseFloat(item.lat), parseFloat(item.lon)];
    setQuery(name);
    setOpen(false);
    setResults([]);
    setSelected({ name, coords });
    onSelect(coords, item.display_name);
  }

  function handleSave() {
    if (!selected) return;
    saveAddress(selected.name, selected.coords);
    setSaved(getSaved());
  }

  function handlePickSaved(item) {
    setQuery(item.name);
    setSelected(item);
    setShowSaved(false);
    onSelect(item.coords, item.name);
  }

  return (
    <div style={s.wrap}>
      <div style={s.inputRow}>
        <input
          style={s.input}
          value={query}
          onChange={e => { setQuery(e.target.value); setSelected(null); }}
          placeholder={placeholder}
          onFocus={() => results.length > 0 && setOpen(true)}
        />
        {loading && <span style={s.spinner}>...</span>}
        {query && !loading && (
          <button style={s.clear} onClick={() => { setQuery(''); setOpen(false); setSelected(null); }}>×</button>
        )}
        {/* כפתור שמירת כתובת */}
        {selected && (
          <button style={s.star} title="שמור כתובת" onClick={handleSave}>★</button>
        )}
        {/* כפתור כתובות שמורות */}
        {saved.length > 0 && (
          <button style={s.bookmark} title="כתובות שמורות" onClick={() => setShowSaved(v => !v)}>♥</button>
        )}
      </div>

      {open && (
        <ul style={s.dropdown}>
          {results.map((r, i) => (
            <li key={i} style={s.item} onMouseDown={() => handleSelect(r)}>
              <span style={s.icon}>•</span>
              <span style={s.name}>{r.display_name}</span>
            </li>
          ))}
        </ul>
      )}

      {showSaved && (
        <ul style={s.dropdown}>
          <li style={{ ...s.item, color: '#888', fontSize: 11, cursor: 'default' }}>כתובות שמורות:</li>
          {saved.map((a, i) => (
            <li key={i} style={s.item} onMouseDown={() => handlePickSaved(a)}>
              <span style={s.icon}>★</span>
              <span style={s.name}>{a.name}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

const s = {
  wrap:      { position: 'relative', width: '100%' },
  inputRow:  { display: 'flex', alignItems: 'center', gap: 4 },
  input: {
    flex: 1, padding: '8px 10px', borderRadius: 8,
    border: '1.5px solid #86efac', fontSize: 13,
    outline: 'none', direction: 'rtl',
  },
  spinner:   { fontSize: 13 },
  star: {
    background: 'none', border: 'none', cursor: 'pointer',
    fontSize: 15, color: '#f59e0b', padding: '0 3px',
  },
  bookmark: {
    background: 'none', border: 'none', cursor: 'pointer',
    fontSize: 14, color: '#ec4899', padding: '0 3px',
  },
  clear: {
    background: 'none', border: 'none', cursor: 'pointer',
    fontSize: 14, color: '#888', padding: '0 4px',
  },
  dropdown: {
    position: 'absolute', top: '100%', right: 0, left: 0, zIndex: 9999,
    background: '#fff', border: '1px solid #86efac', borderRadius: 8,
    marginTop: 2, padding: 0, listStyle: 'none',
    boxShadow: '0 4px 12px rgba(0,0,0,0.12)', maxHeight: 220, overflowY: 'auto',
  },
  item: {
    display: 'flex', alignItems: 'flex-start', gap: 6,
    padding: '8px 10px', cursor: 'pointer', fontSize: 12,
    borderBottom: '1px solid #f0fdf4', direction: 'rtl',
  },
  icon:      { flexShrink: 0, marginTop: 1 },
  name:      { color: '#166534', lineHeight: 1.4 },
};
