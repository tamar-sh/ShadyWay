import { useState } from 'react';
import { login, register } from '../api/authApi';

export default function AuthForm({ onAuthenticated }) {
  const [mode, setMode]                   = useState('login');
  const [email, setEmail]                 = useState('');
  const [password, setPassword]           = useState('');
  const [fullName, setFullName]           = useState('');
  const [idCard, setIdCard]               = useState('');
  const [shadowPreference, setShadowPref] = useState(1.2);
  const [error, setError]                 = useState(null);
  const [loading, setLoading]             = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const data = mode === 'login'
        ? await login(email, password)
        : await register(idCard, fullName, email, password, shadowPreference);

      sessionStorage.setItem('token', data.token);
      onAuthenticated(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={styles.overlay}>
      <div style={styles.card}>
        <h2 style={styles.title}>ShadyWay</h2>
        <p style={styles.subtitle}>ניווט מוצל להולכי רגל</p>

        <div style={styles.tabs}>
          <button
            style={{ ...styles.tab, ...(mode === 'login'    ? styles.activeTab : {}) }}
            onClick={() => setMode('login')}
          >כניסה</button>
          <button
            style={{ ...styles.tab, ...(mode === 'register' ? styles.activeTab : {}) }}
            onClick={() => setMode('register')}
          >הרשמה</button>
        </div>

        <form onSubmit={handleSubmit} style={styles.form}>
          {mode === 'register' && (
            <input style={styles.input} placeholder="תעודת זהות" value={idCard}
              onChange={e => setIdCard(e.target.value)} required />
          )}

          {mode === 'register' && (
            <input style={styles.input} placeholder="שם מלא" value={fullName}
              onChange={e => setFullName(e.target.value)} required />
          )}

          <input style={styles.input} type="email" placeholder="אימייל" value={email}
            onChange={e => setEmail(e.target.value)} required />
          <input style={styles.input} type="password" placeholder="סיסמה" value={password}
            onChange={e => setPassword(e.target.value)} required />

          {mode === 'register' && (
            <div style={styles.sliderSection}>
              <label style={styles.sliderLabel}>
                העדפת צל: <strong>{((shadowPreference - 1) * 100).toFixed(0)}%</strong> הארכת מסלול
              </label>
              <input type="range" min="1" max="2" step="0.05"
                value={shadowPreference}
                onChange={e => setShadowPref(parseFloat(e.target.value))}
                style={styles.slider} />
              <div style={styles.sliderHints}>
                <span>מסלול קצר</span>
                <span>מסלול מוצל</span>
              </div>
            </div>
          )}

          {error && <p style={styles.error}>{error}</p>}

          <button style={styles.btn} type="submit" disabled={loading}>
            {loading ? 'מתחבר...' : mode === 'login' ? 'כניסה' : 'הרשמה'}
          </button>
        </form>
      </div>
    </div>
  );
}

const styles = {
  overlay: {
    position: 'fixed', inset: 0,
    background: 'rgba(0,0,0,0.4)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 9999,
  },
  card: {
    background: '#fff',
    borderRadius: 12,
    padding: '32px 28px',
    width: 320,
    direction: 'rtl',
    fontFamily: 'Arial, sans-serif',
    display: 'flex',
    flexDirection: 'column',
    gap: 12,
  },
  title:    { margin: 0, color: '#15803d', fontSize: 24, textAlign: 'center' },
  subtitle: { margin: 0, color: '#555', fontSize: 13, textAlign: 'center' },
  tabs: { display: 'flex', gap: 8 },
  tab: {
    flex: 1, padding: '8px 0',
    border: '1px solid #d1d5db',
    borderRadius: 8, cursor: 'pointer',
    background: '#f9fafb', fontSize: 14,
  },
  activeTab: { background: '#15803d', color: '#fff', border: '1px solid #15803d' },
  form:  { display: 'flex', flexDirection: 'column', gap: 10 },
  input: {
    padding: '10px 12px',
    border: '1px solid #d1d5db',
    borderRadius: 8, fontSize: 14,
    outline: 'none', textAlign: 'right',
  },
  btn: {
    padding: '11px 0',
    background: '#15803d', color: '#fff',
    border: 'none', borderRadius: 8,
    fontSize: 15, cursor: 'pointer',
  },
  error:        { color: '#dc2626', fontSize: 13, margin: 0 },
  sliderSection: { display: 'flex', flexDirection: 'column', gap: 4 },
  sliderLabel:   { fontSize: 13, color: '#374151' },
  slider:        { width: '100%', accentColor: '#15803d' },
  sliderHints:   { display: 'flex', justifyContent: 'space-between', fontSize: 11, color: '#9ca3af' },
};
