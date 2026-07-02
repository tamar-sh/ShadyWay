import { useState } from 'react';
import RouteHistory from './RouteHistory';
import AddressSearch from './AddressSearch';

export default function RouteForm({
  startPoint,
  endPoint,
  shadowPreference,
  onShadowChange,
  onPickStart,
  onPickEnd,
  onSetStart,
  onSetEnd,
  onCalculate,
  loading,
  result,
  error,
  useCustomTime,
  onToggleCustomTime,
  customDateTime,
  onCustomDateTimeChange,
  history,
  onSelectHistory,
  currentUser,
  onLogout,
}) {
  const [showHistory, setShowHistory] = useState(false);
  return (
    <div style={styles.panel}>
      <h2 style={styles.title}>ShadyWay</h2>
      <p style={styles.subtitle}>ניווט מוצל להולכי רגל</p>

      {currentUser && (
        <div style={styles.userBar}>
          <span>{currentUser.fullName}</span>
          <button style={styles.logoutBtn} onClick={onLogout}>יציאה</button>
        </div>
      )}

      <div style={styles.section}>
        <label style={styles.label}>נקודת מוצא</label>
        <AddressSearch
          placeholder="חפש עיר או כתובת..."
          onSelect={(coords, name) => onSetStart(coords, name)}
        />
        {startPoint && (
          <p style={styles.coordNote}>{startPoint[0].toFixed(4)}, {startPoint[1].toFixed(4)}</p>
        )}
        <button
          style={{ ...styles.btnSmall, background: '#2563eb' }}
          onClick={onPickStart}
        >
          {startPoint ? 'שנה על המפה' : 'בחר על המפה'}
        </button>
      </div>

      <div style={styles.section}>
        <label style={styles.label}>יעד</label>
        <AddressSearch
          placeholder="חפש עיר או כתובת..."
          onSelect={(coords, name) => onSetEnd(coords, name)}
        />
        {endPoint && (
          <p style={styles.coordNote}>{endPoint[0].toFixed(4)}, {endPoint[1].toFixed(4)}</p>
        )}
        <button
          style={{ ...styles.btnSmall, background: '#2563eb' }}
          onClick={onPickEnd}
        >
          {endPoint ? 'שנה על המפה' : 'בחר על המפה'}
        </button>
      </div>

      <div style={styles.section}>
        <label style={styles.label}>
          העדפת צל: <strong>{((shadowPreference - 1) * 100).toFixed(0)}%</strong> הארכת מסלול
        </label>
        <input
          type="range"
          min="1"
          max="2"
          step="0.05"
          value={shadowPreference}
          onChange={(e) => onShadowChange(parseFloat(e.target.value))}
          style={styles.slider}
        />
        <div style={styles.sliderLabels}>
          <span>מסלול קצר</span>
          <span>מסלול מוצל</span>
        </div>
      </div>

      <div style={styles.section}>
        <label style={styles.label}>
          <input
            type="checkbox"
            checked={useCustomTime}
            onChange={onToggleCustomTime}
            style={{ marginLeft: 6 }}
          />
          סימולציה — בחר שעה ותאריך
        </label>
        {useCustomTime && (
          <input
            type="datetime-local"
            value={customDateTime}
            onChange={e => onCustomDateTimeChange(e.target.value)}
            style={styles.dateInput}
          />
        )}
        {!useCustomTime && (
          <p style={styles.timeNote}>משתמש בשעה הנוכחית</p>
        )}
      </div>

      <button
        style={{
          ...styles.btn,
          ...styles.calcBtn,
          opacity: loading || !startPoint || !endPoint ? 0.6 : 1,
        }}
        onClick={onCalculate}
        disabled={loading || !startPoint || !endPoint}
      >
        {loading ? 'מחשב מסלול...' : 'חשב מסלול מוצל'}
      </button>

      {error && <p style={styles.error}>{error}</p>}

      {result && result.found && (
        <div style={styles.result}>
          <h3 style={styles.resultTitle}>מסלול נמצא</h3>
          <p>מרחק: <strong>{(result.totalDistanceMeters / 1000).toFixed(2)} ק"מ</strong></p>
          <p>זמן הליכה: <strong>{result.estimatedWalkingTimeMinutes.toFixed(0)} דקות</strong></p>
          <p>צל ממוצע: <strong>{result.averageShadowPercentage.toFixed(0)}%</strong></p>
        </div>
      )}

      {result && !result.found && (
        <p style={styles.error}>לא נמצא מסלול בין הנקודות שנבחרו.</p>
      )}

      <div style={styles.section}>
        <button
          style={{ ...styles.btn, background: '#475569' }}
          onClick={() => setShowHistory(v => !v)}
        >
          {showHistory ? 'הסתר היסטוריה' : `מסלולים קודמים (${history?.length ?? 0})`}
        </button>
        {showHistory && (
          <RouteHistory history={history} onSelect={onSelectHistory} />
        )}
      </div>
    </div>
  );
}

const styles = {
  panel: {
    width: 300,
    padding: 20,
    background: '#fff',
    boxShadow: '2px 0 12px rgba(0,0,0,0.12)',
    display: 'flex',
    flexDirection: 'column',
    gap: 12,
    direction: 'rtl',
    fontFamily: 'Arial, sans-serif',
    zIndex: 1000,
    overflowY: 'auto',
  },
  title:    { margin: 0, color: '#15803d', fontSize: 24 },
  subtitle: { margin: 0, color: '#555', fontSize: 13 },
  section:  { display: 'flex', flexDirection: 'column', gap: 8 },
  btn: {
    padding: '10px 14px',
    border: 'none',
    borderRadius: 8,
    color: '#fff',
    fontSize: 13,
    cursor: 'pointer',
    textAlign: 'right',
  },
  calcBtn:  { background: '#15803d', fontSize: 15, padding: '12px 14px' },
  label:    { fontSize: 13, color: '#333' },
  slider:   { width: '100%', accentColor: '#15803d' },
  sliderLabels: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: 11,
    color: '#888',
  },
  error:     { color: '#dc2626', fontSize: 13 },
  dateInput: { width: '100%', padding: '6px 8px', borderRadius: 6, border: '1px solid #d1d5db', fontSize: 12 },
  timeNote:  { margin: 0, fontSize: 12, color: '#6b7280' },
  result: {
    background: '#f0fdf4',
    border: '1px solid #86efac',
    borderRadius: 8,
    padding: 12,
    fontSize: 13,
    lineHeight: 1.8,
  },
  resultTitle: { margin: '0 0 8px', color: '#15803d' },
  btnSmall: {
    padding: '6px 10px', border: 'none', borderRadius: 6,
    color: '#fff', fontSize: 12, cursor: 'pointer', textAlign: 'right',
    alignSelf: 'flex-start',
  },
  coordNote: { margin: 0, fontSize: 11, color: '#16a34a' },
  userBar: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    background: '#f0fdf4', borderRadius: 8, padding: '6px 10px', fontSize: 13,
  },
  logoutBtn: {
    background: 'none', border: 'none', color: '#dc2626',
    cursor: 'pointer', fontSize: 13,
  },
};
