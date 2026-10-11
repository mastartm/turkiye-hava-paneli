import { useCallback, useMemo, useState } from 'react'
import { BUCKETS, NO_DATA_COLOR, regionAnomalies, regionIdOf } from './anomaly'
import { fetchEarthquakes, fetchMap, fetchSources } from './api'
import { QuakeControls } from './QuakeControls'
import { DEFAULT_QUAKE_SETTINGS, type QuakeSettings } from './quakeSettings'
import { SidePanel, type Selection } from './SidePanel'
import { TurkeyMap, type RegionCollection } from './TurkeyMap'
import type { EarthquakeDto } from './types'
import { useApi } from './useApi'

const REGIONS_URL = `${import.meta.env.BASE_URL}data/turkiye-kktc.geojson`

async function loadRegions(signal: AbortSignal): Promise<RegionCollection> {
  const r = await fetch(REGIONS_URL, { signal })
  if (!r.ok) throw new Error('Harita sınırları yüklenemedi.')
  return (await r.json()) as RegionCollection
}

export default function App() {
  const regions = useApi(loadRegions, 'regions')
  const map = useApi(fetchMap, 'map')
  const sources = useApi(fetchSources, 'sources')
  const [selection, setSelection] = useState<Selection | null>(null)
  const [quakeSettings, setQuakeSettings] = useState<QuakeSettings>(DEFAULT_QUAKE_SETTINGS)
  const [selectedQuake, setSelectedQuake] = useState<EarthquakeDto | null>(null)

  // Katman kapalıyken anahtar null: hiçbir istek atılmaz. Ayar değişince anahtar değişir, yeni sorgu başlar.
  const quakeKey = quakeSettings.enabled ? `${quakeSettings.days}-${quakeSettings.minMagnitude}` : null
  const quakes = useApi(
    (signal) => fetchEarthquakes(quakeSettings.days, quakeSettings.minMagnitude, signal),
    quakeKey,
  )
  const quakeList = useMemo(() => (quakeSettings.enabled ? (quakes.data ?? []) : []), [quakeSettings.enabled, quakes.data])
  const quakeStatus = quakes.loading ? 'Yükleniyor…' : (quakes.error ?? `${quakeList.length} deprem`)

  const changeQuakeSettings = useCallback((next: QuakeSettings) => {
    setQuakeSettings(next)
    setSelectedQuake(null) // ayar değişince eski seçim artık listede olmayabilir
  }, [])

  const points = useMemo(() => map.data?.points ?? [], [map.data])
  const anomalies = useMemo(() => regionAnomalies(points), [points])

  const selectRegion = useCallback((regionId: string) => {
    setSelectedQuake(null)
    setSelection({ regionId, cityId: regionId === 'kktc' ? null : regionId })
  }, [])
  const selectCity = useCallback(
    (cityId: string) => {
      setSelectedQuake(null)
      setSelection({ regionId: regionIdOf(cityId, points), cityId })
    },
    [points],
  )
  const backToRegion = useCallback(
    () => setSelection((s) => (s ? { regionId: s.regionId, cityId: null } : s)),
    [],
  )

  const failure = regions.error ?? map.error

  return (
    <div className="app">
      <header>
        <h1>Türkiye hava durumu paneli</h1>
        <p className="muted">
          Bugün normalden ne kadar sıcak ya da soğuk? Renk, bugünün günlük ortalama sıcaklığının 1991-2020 normalinden sapmasını gösterir.
          {map.data && <> Tarih: {map.data.date}.</>}
        </p>
      </header>

      <main>
        <section className="map-card" aria-label="Harita">
          <QuakeControls settings={quakeSettings} onChange={changeQuakeSettings} status={quakeStatus} />

          {failure ? (
            <p className="error" role="alert">{failure}</p>
          ) : regions.loading || map.loading || !regions.data ? (
            <p className="muted" role="status">Harita yükleniyor…</p>
          ) : (
            <TurkeyMap
              regions={regions.data}
              anomalies={anomalies}
              selectedId={selection?.regionId ?? null}
              onSelect={selectRegion}
              quakes={quakeList}
              selectedQuakeId={selectedQuake?.id ?? null}
              onSelectQuake={setSelectedQuake}
            />
          )}

          <ul className="legend" aria-label="Renk açıklaması">
            {BUCKETS.map((b) => (
              <li key={b.label}><span className="swatch" style={{ background: b.color }} />{b.label}</li>
            ))}
            <li><span className="swatch" style={{ background: NO_DATA_COLOR }} />veri yok</li>
            {quakeSettings.enabled && (
              <>
                <li><span className="swatch swatch-quake" />deprem (daire boyutu: büyüklük)</li>
                <li><span className="swatch swatch-cluster" />iç içe depremler (tıklayınca yayılır)</li>
              </>
            )}
          </ul>
        </section>

        <SidePanel
          selection={selection}
          points={points}
          onSelectCity={selectCity}
          onBackToRegion={backToRegion}
          quake={selectedQuake}
          onCloseQuake={() => setSelectedQuake(null)}
        />
      </main>

      <footer className="muted">
        Veri:{' '}
        {sources.data
          ? sources.data.map((s, i) => (
              <span key={s.name}>
                {i > 0 && ', '}
                <a href={s.url} target="_blank" rel="noreferrer">{s.name}</a> ({s.license})
              </span>
            ))
          : 'Open-Meteo (CC BY 4.0), USGS'}
        . Harita: <a href="https://www.naturalearthdata.com/" target="_blank" rel="noreferrer">Made with Natural Earth</a>.
        {quakeSettings.enabled && ' Depremler USGS verisidir; küçük depremler eksik olabilir.'}
      </footer>
    </div>
  )
}
