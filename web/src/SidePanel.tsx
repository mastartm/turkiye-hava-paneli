import { colorFor, formatAnomaly, formatC } from './anomaly'
import { fetchForecast } from './api'
import { KKTC_REGION_ID, type DayDto, type MapPointDto } from './types'
import { useApi } from './useApi'
import { shortDay, weatherLabel } from './weather'

export interface Selection {
  /** Haritada vurgulanan bölge: il kimliği ya da 'kktc'. */
  regionId: string
  /** Tahmini gösterilen şehir; KKTC'de bir ilçe seçilene kadar null. */
  cityId: string | null
}

interface Props {
  selection: Selection | null
  points: readonly MapPointDto[]
  onSelectCity: (cityId: string) => void
  onBackToRegion: () => void
}

export function SidePanel({ selection, points, onSelectCity, onBackToRegion }: Props) {
  if (!selection) {
    return (
      <aside className="panel panel-empty">
        <h2>Bir il seçin</h2>
        <p>Haritada bir ile tıklayın; 7 günlük tahmini ve normalden sapmasını burada görün.</p>
      </aside>
    )
  }

  if (selection.regionId === KKTC_REGION_ID && selection.cityId === null) {
    const districts = points.filter((p) => p.city.country === 'Kktc')
    return (
      <aside className="panel">
        <h2>KKTC</h2>
        <p className="muted">Haritada tek bölge; ilçeleri ayrı ayrı görmek için seçin.</p>
        <ul className="district-list">
          {districts.map((p) => (
            <li key={p.city.id}>
              <button type="button" onClick={() => onSelectCity(p.city.id)}>
                <span>{p.city.name}</span>
                <span className="muted">{formatC(p.meanC)}</span>
                <span className="chip" style={{ background: colorFor(p.anomalyC) }}>
                  {formatAnomaly(p.anomalyC)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      </aside>
    )
  }

  const cityId = selection.cityId ?? selection.regionId
  const isDistrict = selection.regionId === KKTC_REGION_ID
  return (
    <aside className="panel">
      {isDistrict && (
        <button type="button" className="back" onClick={onBackToRegion}>
          ← KKTC
        </button>
      )}
      <Forecast cityId={cityId} />
    </aside>
  )
}

function Forecast({ cityId }: { cityId: string }) {
  const { data, error, loading } = useApi((signal) => fetchForecast(cityId, signal), cityId)

  if (loading) return <p className="muted" role="status">Yükleniyor…</p>
  if (error) return <p className="error" role="alert">{error}</p>
  if (!data) return null

  const today = data.days[0] ?? null
  return (
    <>
      <h2>{data.city.name}</h2>
      <p className="muted">{data.city.country === 'Kktc' ? 'KKTC' : 'Türkiye'} · {today ? weatherLabel(today.weatherCode) : 'Veri yok'}</p>

      <div className="tiles">
        <Tile label="Bugün" value={formatC(today?.meanC ?? null)} />
        <Tile label="Normal" value={formatC(today?.normalC ?? null)} />
        <Tile label="Sapma" value={formatAnomaly(today?.anomalyC ?? null)} color={colorFor(today?.anomalyC ?? null)} />
      </div>

      {data.days.length === 0 ? (
        <p className="muted">Bu şehir için henüz tahmin verisi yok.</p>
      ) : (
        <DayList days={data.days} />
      )}
    </>
  )
}

function Tile({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="tile" style={color ? { borderTop: `4px solid ${color}` } : undefined}>
      <span className="muted">{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function DayList({ days }: { days: readonly DayDto[] }) {
  // Çubuk uzunlukları aynı ölçekte olsun diye pencerenin en yüksek/en düşük değeri üzerinden ölçeklenir.
  const lows = days.map((d) => d.minC).filter((v): v is number => v !== null)
  const highs = days.map((d) => d.maxC).filter((v): v is number => v !== null)
  const lo = Math.min(...lows, 0)
  const hi = Math.max(...highs, 1)
  const span = hi - lo || 1

  return (
    <ul className="days" aria-label="7 günlük tahmin">
      {days.map((d) => {
        const left = d.minC === null ? 0 : ((d.minC - lo) / span) * 100
        const right = d.maxC === null ? 0 : ((d.maxC - lo) / span) * 100
        return (
          <li key={d.date}>
            <span className="day-name">{shortDay(d.date)}</span>
            <span className="muted day-temp">{formatC(d.minC)}</span>
            <span className="bar" aria-hidden="true">
              {d.minC !== null && d.maxC !== null && (
                <span style={{ left: `${left}%`, width: `${Math.max(right - left, 2)}%`, background: colorFor(d.anomalyC) }} />
              )}
            </span>
            <span className="day-temp">{formatC(d.maxC)}</span>
          </li>
        )
      })}
    </ul>
  )
}
