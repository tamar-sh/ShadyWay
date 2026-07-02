import { useState, useEffect } from 'react';
import MapView from './components/MapView';
import RouteForm from './components/RouteForm';
import AuthForm from './components/AuthForm';
import { calculateRoute, getRouteHistory } from './api/shadyWayApi';

export default function App() {
  const [startPoint, setStartPoint]       = useState(null);  // [lat, lon]
  const [endPoint, setEndPoint]           = useState(null);
  const [startName, setStartName]         = useState('');
  const [endName, setEndName]             = useState('');
  const [shadowPreference, setShadowPref] = useState(() => {
    const saved = localStorage.getItem('shadowPreference');
    return saved ? parseFloat(saved) : 1.2;
  });
  const [pickingMode, setPickingMode]     = useState(null);  // 'start' | 'end' | null
  const [routePath, setRoutePath]         = useState([]);
  const [segmentShadows, setSegmentShadows] = useState([]); // אחוז צל לכל קטע ב-routePath
  const [result, setResult]               = useState(null);
  const [loading, setLoading]             = useState(false);
  const [error, setError]                 = useState(null);
  const [useCustomTime, setUseCustomTime] = useState(false);
  const [customDateTime, setCustomDateTime] = useState('');
  const [history, setHistory]     = useState([]);
  const [currentUser, setCurrentUser] = useState(() => {
    const token = sessionStorage.getItem('token');
    const name  = sessionStorage.getItem('userName');
    return token ? { token, fullName: name } : null;
  });

  useEffect(() => {
    if (currentUser) {
      getRouteHistory(currentUser.token).then(setHistory).catch(() => {});
    }
  }, [currentUser]);

  function handleAuthenticated(data) {
    sessionStorage.setItem('userName', data.fullName);
    localStorage.setItem('shadowPreference', data.shadowPreference);
    setCurrentUser({ token: data.token, fullName: data.fullName });
    setShadowPref(data.shadowPreference);
  }

  function handleLogout() {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('userName');
    localStorage.removeItem('shadowPreference');
    setCurrentUser(null);
    setHistory([]);
    setShadowPref(1.2);
  }

  async function handleSelectHistory(item) {
    const [sLat, sLon] = item.sourceAddress.split(',').map(Number);
    const [eLat, eLon] = item.destAddress.split(',').map(Number);
    setStartPoint([sLat, sLon]);
    setEndPoint([eLat, eLon]);
    // מחשב מיד עם השמש הנוכחית
    await runCalculation(sLat, sLon, eLat, eLon);
  }

  function handleMapClick(latlng) {
    const point = [latlng.lat, latlng.lng];
    if (pickingMode === 'start') {
      setStartPoint(point);
      setPickingMode(null);
      setRoutePath([]);
      setSegmentShadows([]);
      setResult(null);
    } else if (pickingMode === 'end') {
      setEndPoint(point);
      setPickingMode(null);
      setRoutePath([]);
      setSegmentShadows([]);
      setResult(null);
    }
  }

  async function handleCalculate() {
    if (!startPoint || !endPoint) return;
    await runCalculation(startPoint[0], startPoint[1], endPoint[0], endPoint[1]);
  }

  function cacheAddressName(lat, lon, name) {
    if (!name) return;
    const key   = `${parseFloat(lat).toFixed(6)},${parseFloat(lon).toFixed(6)}`;
    const cache = JSON.parse(localStorage.getItem('shadyway_addr_cache') || '{}');
    cache[key]  = name;
    localStorage.setItem('shadyway_addr_cache', JSON.stringify(cache));
  }

  async function runCalculation(sLat, sLon, eLat, eLon) {
    setLoading(true);
    setError(null);
    setResult(null);
    setRoutePath([]);
    setSegmentShadows([]);

    try {
      const utcDateTime = useCustomTime && customDateTime
        ? new Date(customDateTime).toISOString()
        : null;

      cacheAddressName(sLat, sLon, startName);
      cacheAddressName(eLat, eLon, endName);
      const data = await calculateRoute(sLat, sLon, eLat, eLon, shadowPreference, utcDateTime, currentUser.token);
      setResult(data);
      if (data.found && data.path) {
        setRoutePath(data.path.map(c => [c.latitude, c.longitude]));
        setSegmentShadows(data.segmentShadowPercentages || []);
      }
      // מרענן את ההיסטוריה אחרי כל חישוב מוצלח
      getRouteHistory(currentUser.token).then(setHistory).catch(() => {});
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ display: 'flex', height: '100vh', width: '100vw' }}>
      {!currentUser && <AuthForm onAuthenticated={handleAuthenticated} />}
      <RouteForm
        currentUser={currentUser}
        onLogout={handleLogout}
        startPoint={startPoint}
        endPoint={endPoint}
        shadowPreference={shadowPreference}
        onShadowChange={setShadowPref}
        onPickStart={() => setPickingMode('start')}
        onPickEnd={() => setPickingMode('end')}
        onSetStart={(coords, name) => { setStartPoint(coords); setStartName(name || ''); setPickingMode(null); setRoutePath([]); setSegmentShadows([]); setResult(null); }}
        onSetEnd={(coords, name)   => { setEndPoint(coords);   setEndName(name || '');   setPickingMode(null); setRoutePath([]); setSegmentShadows([]); setResult(null); }}
        onCalculate={handleCalculate}
        loading={loading}
        result={result}
        error={error}
        useCustomTime={useCustomTime}
        onToggleCustomTime={() => setUseCustomTime(v => !v)}
        customDateTime={customDateTime}
        onCustomDateTimeChange={setCustomDateTime}
        history={history}
        onSelectHistory={handleSelectHistory}
      />

      <div style={{ flex: 1, position: 'relative' }}>
        {pickingMode && (
          <div style={bannerStyle}>
            {pickingMode === 'start'
              ? 'לחץ על המפה לבחירת נקודת מוצא'
              : 'לחץ על המפה לבחירת יעד'}
          </div>
        )}
        <MapView
          startPoint={startPoint}
          endPoint={endPoint}
          routePath={routePath}
          segmentShadows={segmentShadows}
          pickingMode={pickingMode}
          onMapClick={handleMapClick}
        />
      </div>
    </div>
  );
}

const bannerStyle = {
  position: 'absolute',
  top: 12,
  left: '50%',
  transform: 'translateX(-50%)',
  background: '#1d4ed8',
  color: '#fff',
  padding: '8px 18px',
  borderRadius: 20,
  fontSize: 14,
  zIndex: 1000,
  pointerEvents: 'none',
};
