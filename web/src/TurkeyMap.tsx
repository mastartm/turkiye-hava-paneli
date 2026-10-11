import { geoMercator, geoPath } from 'd3-geo'
import type { Feature, FeatureCollection, Geometry } from 'geojson'
import { useEffect, useMemo, useState } from 'react'
import { colorFor, formatAnomaly } from './anomaly'
import { ClusterCard } from './ClusterCard'
import { clusterQuakes, fanLayout } from './clusters'
import { describeQuake, isInView, radiusFor, sortForDrawing, type PlacedQuake } from './earthquakes'
import { regionNameAt } from './location'
import { QuakeCard } from './QuakeCard'
import type { EarthquakeDto } from './types'
import { useElementSize } from './useElementSize'

export interface RegionProps {
  id: string
  name: string
  country: string
}

export type RegionCollection = FeatureCollection<Geometry, RegionProps>

interface Props {
  regions: RegionCollection
  anomalies: ReadonlyMap<string, number | null>
  selectedId: string | null
  onSelect: (regionId: string) => void
  /** Çizilecek depremler; boş dizi = katman kapalı ya da deprem yok. */
  quakes: readonly EarthquakeDto[]
  selectedQuakeId: string | null
  onSelectQuake: (quake: EarthquakeDto) => void
}

const WIDTH = 960
const HEIGHT = 440
const VIEW = { width: WIDTH, height: HEIGHT }

/**
 * Türkiye (81 il) + KKTC haritası, GeoJSON'dan doğrudan SVG olarak çizilir.
 * Renk: bölgenin normalden sapması. Üzerine gelince bölge yükselir, tıklayınca seçilir.
 * Depremler illerin üstünde, büyüklüğe göre boyutlanan yarı saydam dairelerdir. İç içe giren depremler tek bir
 * küme işaretinde toplanır; kümeye tıklayınca üyeler etrafa yayılır, merkez işarete/Esc'ye basınca toplanır.
 */
export function TurkeyMap({ regions, anomalies, selectedId, onSelect, quakes, selectedQuakeId, onSelectQuake }: Props) {
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [hoveredQuakeId, setHoveredQuakeId] = useState<string | null>(null)
  const [hoveredClusterId, setHoveredClusterId] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [stageRef, stageSize] = useElementSize<HTMLDivElement>()

  // Projeksiyon ve yol hesabı pahalı: yalnızca sınır verisi değişince yeniden hesaplanır.
  const { paths, project } = useMemo(() => {
    const projection = geoMercator().fitExtent(
      [
        [8, 8],
        [WIDTH - 8, HEIGHT - 8],
      ],
      regions,
    )
    const toPath = geoPath(projection)
    return {
      paths: regions.features.map((f: Feature<Geometry, RegionProps>) => ({ props: f.properties, d: toPath(f) ?? '' })),
      project: (lon: number, lat: number) => projection([lon, lat]),
    }
  }, [regions])

  // Harita çerçevesinin dışına taşan depremler (komşu ülke/açık deniz) çizilmez, sayılır ve altta belirtilir.
  const { drawn, hiddenCount } = useMemo(() => {
    const placed: PlacedQuake[] = []
    let hidden = 0
    for (const q of sortForDrawing(quakes)) {
      const xy = project(q.longitude, q.latitude)
      if (xy && isInView(xy[0], xy[1], WIDTH, HEIGHT)) placed.push({ quake: q, x: xy[0], y: xy[1], r: radiusFor(q.magnitude) })
      else hidden++
    }
    return { drawn: placed, hiddenCount: hidden }
  }, [quakes, project])

  const { singles, clusters } = useMemo(() => clusterQuakes(drawn), [drawn])

  // Açık küme yoksa (veri/ayar değişince kayboldu) kapalı sayılır; durum ayrıca temizlenmesine gerek yok.
  const expanded = clusters.find((c) => c.id === expandedId) ?? null
  const fan = useMemo(
    () => (expanded ? fanLayout({ x: expanded.x, y: expanded.y }, expanded.members, VIEW) : []),
    [expanded],
  )

  // Fare altında bulunabilecek tek daireler: tekler + açık kümenin yayılmış üyeleri (yayılmış konumlarıyla).
  const visibleQuakes = useMemo<PlacedQuake[]>(
    () => [...singles, ...fan.map((f) => ({ quake: f.quake.quake, x: f.x, y: f.y, r: f.quake.r }))],
    [singles, fan],
  )

  // Esc ile açık kümeyi topla (yalnızca açıkken dinlenir).
  useEffect(() => {
    if (!expandedId) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setExpandedId(null)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [expandedId])

  const toggleCluster = (id: string) => {
    setHoveredClusterId(null)
    setExpandedId((current) => (current === id ? null : id))
  }

  const hovered = paths.find((p) => p.props.id === hoveredId) ?? null
  const hoveredQuake = visibleQuakes.find((p) => p.quake.id === hoveredQuakeId) ?? null
  const hoveredCluster = clusters.find((c) => c.id === hoveredClusterId && c.id !== expandedId) ?? null

  const quakeRegion = useMemo(
    () => (hoveredQuake ? regionNameAt(regions, hoveredQuake.quake.longitude, hoveredQuake.quake.latitude) : null),
    [hoveredQuake, regions],
  )
  const clusterRegion = useMemo(() => {
    if (!hoveredCluster) return null
    const n = hoveredCluster.members.length
    const lon = hoveredCluster.members.reduce((s, m) => s + m.quake.longitude, 0) / n
    const lat = hoveredCluster.members.reduce((s, m) => s + m.quake.latitude, 0) / n
    return regionNameAt(regions, lon, lat)
  }, [hoveredCluster, regions])

  const label = (id: string, name: string) => `${name}, sapma ${formatAnomaly(anomalies.get(id) ?? null)}`
  const scale = stageSize.width / WIDTH

  const quakeHandlers = (p: PlacedQuake) => ({
    onMouseEnter: () => setHoveredQuakeId(p.quake.id),
    onMouseLeave: () => setHoveredQuakeId(null),
    onFocus: () => setHoveredQuakeId(p.quake.id),
    onBlur: () => setHoveredQuakeId(null),
    onClick: () => onSelectQuake(p.quake),
    onKeyDown: (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        onSelectQuake(p.quake)
      }
    },
  })

  return (
    <div className="map-wrap">
      <p className="map-caption" aria-live="polite">
        {hoveredQuake ? (
          <strong>{describeQuake(hoveredQuake.quake)}</strong>
        ) : hoveredCluster ? (
          <strong>{hoveredCluster.members.length} deprem iç içe · tıklayınca açılır</strong>
        ) : hovered ? (
          <>
            <strong>{hovered.props.name}</strong> · sapma {formatAnomaly(anomalies.get(hovered.props.id) ?? null)}
          </>
        ) : expanded ? (
          'Kapatmak için ortadaki işarete tıklayın ya da Esc tuşuna basın'
        ) : (
          'Bir şehrin üzerine gelin ya da tıklayın'
        )}
      </p>

      <div className="map-stage" ref={stageRef}>
        <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="group" aria-label="Türkiye ve KKTC haritası">
          <g className="regions">
            {paths.map(({ props, d }) => (
              <path
                key={props.id}
                d={d}
                className={`region${props.id === selectedId ? ' selected' : ''}`}
                fill={colorFor(anomalies.get(props.id) ?? null)}
                role="button"
                tabIndex={0}
                aria-label={label(props.id, props.name)}
                aria-pressed={props.id === selectedId}
                onMouseEnter={() => setHoveredId(props.id)}
                onMouseLeave={() => setHoveredId(null)}
                onFocus={() => setHoveredId(props.id)}
                onBlur={() => setHoveredId(null)}
                onClick={() => onSelect(props.id)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    onSelect(props.id)
                  }
                }}
              />
            ))}
          </g>

          {/* Yükselen kopya: fare olaylarını almaz (pointer-events: none), böylece alttaki yol yerinde kalır
              ve elemanlar yer değiştirirken oluşan "hover titremesi" olmaz. */}
          {hovered && (
            <path
              key={hovered.props.id}
              d={hovered.d}
              className="region-lift"
              fill={colorFor(anomalies.get(hovered.props.id) ?? null)}
              pointerEvents="none"
            />
          )}

          {/* Açık kümenin yayılması: her üyeden depremin GERÇEK konumuna çizgi ve küçük bir nokta. */}
          {expanded && (
            <g className="fan-lines" pointerEvents="none">
              {fan.map(({ quake: m, x, y }) => (
                <g key={m.quake.id}>
                  <line x1={x} y1={y} x2={m.x} y2={m.y} />
                  <circle cx={m.x} cy={m.y} r={2.4} />
                </g>
              ))}
            </g>
          )}

          {/* Depremler en üstte. Büyükten küçüğe çizildiği için küçükler büyüklerin altında kalmaz. */}
          <g className="quakes">
            {visibleQuakes.map((p) => (
              <circle
                key={p.quake.id}
                cx={p.x}
                cy={p.y}
                r={p.r}
                className={`quake${p.quake.id === selectedQuakeId ? ' selected' : ''}`}
                role="button"
                tabIndex={0}
                aria-label={describeQuake(p.quake)}
                {...quakeHandlers(p)}
              />
            ))}
          </g>

          {/* Küme işaretleri (magenta, içinde deprem sayısı). Açıkken sönük bir halka olur ve gerçek konum
              noktalarının üstünü örtmez; tıklayınca küme toplanır. */}
          <g className="clusters">
            {clusters.map((c) => {
              const open = c.id === expandedId
              return (
                <g key={c.id}>
                  <circle
                    cx={c.x}
                    cy={c.y}
                    r={c.r}
                    className={`cluster${open ? ' open' : ''}`}
                    role="button"
                    tabIndex={0}
                    aria-expanded={open}
                    aria-label={`${c.members.length} deprem iç içe, ${open ? 'toplamak' : 'açmak'} için tıklayın`}
                    onMouseEnter={() => setHoveredClusterId(c.id)}
                    onMouseLeave={() => setHoveredClusterId(null)}
                    onFocus={() => setHoveredClusterId(c.id)}
                    onBlur={() => setHoveredClusterId(null)}
                    onClick={() => toggleCluster(c.id)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault()
                        toggleCluster(c.id)
                      }
                    }}
                  />
                  {!open && (
                    <text x={c.x} y={c.y} className="cluster-count" pointerEvents="none">
                      {c.members.length}
                    </text>
                  )}
                </g>
              )
            })}
          </g>
        </svg>

        {hoveredQuake && (
          <QuakeCard placed={hoveredQuake} regionName={quakeRegion} container={stageSize} scale={scale} />
        )}
        {!hoveredQuake && hoveredCluster && (
          <ClusterCard cluster={hoveredCluster} regionName={clusterRegion} container={stageSize} scale={scale} />
        )}
      </div>

      {hiddenCount > 0 && (
        <p className="muted map-note">{hiddenCount} deprem harita çerçevesinin dışında kaldı (komşu bölgeler).</p>
      )}
    </div>
  )
}
