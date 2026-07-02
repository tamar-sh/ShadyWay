import { useEffect } from 'react';
import { MapContainer, TileLayer, Polyline, Marker, Popup, useMapEvents, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// תיקון ל-icon של Leaflet שנשבר ב-Webpack/Vite
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl:       'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl:     'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// ממקם את המפה למיקום הנוכחי של המשתמש בטעינה ראשונה
function CurrentLocationPan({ hasPoints }) {
  const map = useMap();
  useEffect(() => {
    if (hasPoints) return;
    navigator.geolocation?.getCurrentPosition(
      ({ coords }) => map.flyTo([coords.latitude, coords.longitude], 15),
      () => {} // אם נדחה — נשאר על ברירת המחדל
    );
  }, []);
  return null;
}

// קומפוננטה פנימית שמאזינה לקליקים על המפה לבחירת נקודות
function ClickHandler({ pickingMode, onMapClick }) {
  useMapEvents({
    click(e) {
      if (pickingMode) {
        onMapClick(e.latlng);
      }
    },
  });
  return null;
}

// מזיז את המפה בכל פעם שהנקודות או המסלול משתנים
// (MapContainer מזיז את המפה רק פעם אחת בעת היצירה, props אחר כך לא מזיזים אותה)
function MapUpdater({ startPoint, endPoint, routePath }) {
  const map = useMap();
  useEffect(() => {
    if (routePath.length > 1) {
      map.fitBounds(routePath, { padding: [40, 40] });
    } else if (startPoint && endPoint) {
      map.fitBounds([startPoint, endPoint], { padding: [60, 60] });
    } else if (startPoint) {
      map.flyTo(startPoint, 15);
    } else if (endPoint) {
      map.flyTo(endPoint, 15);
    }
  }, [startPoint, endPoint, routePath]);
  return null;
}

// סף לסיווג קטע כ"מוצלל" — מעל זה ירוק, מתחת זה כתום
const SHADE_THRESHOLD = 50;
const SHADED_COLOR     = '#16a34a'; // ירוק — קטע מוצלל
const UNSHADED_COLOR   = '#f59e0b'; // כתום — קטע לא מוצלל

export default function MapView({ startPoint, endPoint, routePath, segmentShadows = [], pickingMode, onMapClick }) {
  const center = startPoint ?? [31.7767, 35.2345];

  // אם יש נתוני צל לכל קטע — מציירים קטע-קטע צבוע לפי אחוז הצל.
  // אם אין (למשל מסלול ישן בלי הנתון) — נופלים לקו ירוק אחיד כברירת מחדל.
  const hasSegmentData = segmentShadows.length === routePath.length - 1;

  return (
    <MapContainer
      center={center}
      zoom={14}
      style={{ height: '100%', width: '100%', cursor: pickingMode ? 'crosshair' : 'grab' }}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />

      <CurrentLocationPan hasPoints={!!(startPoint || endPoint)} />
      <ClickHandler pickingMode={pickingMode} onMapClick={onMapClick} />
      <MapUpdater startPoint={startPoint} endPoint={endPoint} routePath={routePath} />

      {startPoint && (
        <Marker position={startPoint}>
          <Popup>נקודת מוצא</Popup>
        </Marker>
      )}

      {endPoint && (
        <Marker position={endPoint}>
          <Popup>יעד</Popup>
        </Marker>
      )}

      {/* המסלול — צבוע קטע-קטע לפי אחוז הצל, או ירוק אחיד אם אין נתוני צל */}
      {routePath.length > 1 && hasSegmentData && routePath.slice(0, -1).map((point, i) => (
        <Polyline
          key={i}
          positions={[point, routePath[i + 1]]}
          color={segmentShadows[i] > SHADE_THRESHOLD ? SHADED_COLOR : UNSHADED_COLOR}
          weight={5}
          opacity={0.85}
        />
      ))}
      {routePath.length > 1 && !hasSegmentData && (
        <Polyline positions={routePath} color={SHADED_COLOR} weight={5} opacity={0.85} />
      )}
    </MapContainer>
  );
}
