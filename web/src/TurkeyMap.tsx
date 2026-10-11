import { geoMercator, geoPath } from 'd3-geo'
import type { Feature, FeatureCollection, Geometry } from 'geojson'
import { useMemo, useState } from 'react'
import { colorFor, formatAnomaly } from './anomaly'
import { describeQuake, isInView, overlappingWith, radiusFor, sortForDrawing, type PlacedQuake } from './earthquakes'
import { regionNameAt } from './location'
import { QuakeCard } from './QuakeCard'
import { useElementSize } from './useElementSize'
import type { EarthquakeDto } from './types'

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

/**
 * Türkiye (81 il) + KKTC haritası, GeoJSON'dan doğrudan SVG olarak çizilir.
 * Renk: bölgenin normalden sapması. Üzerine gelince bölge yükselir, tıklayınca seçilir.
 * Depremler illerin üstünde, büyüklüğe göre boyutlanan yarı saydam dairelerdir.
 */
export function TurkeyMap({ regions, anomalies, selectedId, onSelect, quakes, selectedQuakeId, onSelectQuake }: Props) {
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [hoveredQuake, setHoveredQuake] = useState<EarthquakeDto | null>(null)
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

  const hovered = paths.find((p) => p.props.id === hoveredId) ?? null

  // Hover kartı: fare altındaki deprem, onunla üst üste binenler ve içinde bulunduğu il.
  const hoveredPlaced = hoveredQuake ? (drawn.find((d) => d.quake.id === hoveredQuake.id) ?? null) : null
  const group = useMemo(() => (hoveredPlaced ? overlappingWith(hoveredPlaced, drawn) : []), [hoveredPlaced, drawn])
  const hoveredRegionName = useMemo(
    () => (hoveredQuake ? regionNameAt(regions, hoveredQuake.longitude, hoveredQuake.latitude) : null),
    [hoveredQuake, regions],
  )

  const label = (id: string, name: string) => `${name}, sapma ${formatAnomaly(anomalies.get(id) ?? null)}`

  return (
    <div className="map-wrap">
      <p className="map-caption" aria-live="polite">
        {hoveredQuake ? (
          <strong>{describeQuake(hoveredQuake)}</strong>
        ) : hovered ? (
          <>
            <strong>{hovered.props.name}</strong> · sapma {formatAnomaly(anomalies.get(hovered.props.id) ?? null)}
          </>
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

        {/* Depremler en üstte. Büyükten küçüğe çizildiği için küçükler büyüklerin altında kalmaz. */}
        <g className="quakes">
          {drawn.map(({ quake, x, y, r }) => (
            <circle
              key={quake.id}
              cx={x}
              cy={y}
              r={r}
              className={`quake${quake.id === selectedQuakeId ? ' selected' : ''}`}
              role="button"
              tabIndex={0}
              aria-label={describeQuake(quake)}
              onMouseEnter={() => setHoveredQuake(quake)}
              onMouseLeave={() => setHoveredQuake(null)}
              onFocus={() => setHoveredQuake(quake)}
              onBlur={() => setHoveredQuake(null)}
              onClick={() => onSelectQuake(quake)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault()
                  onSelectQuake(quake)
                }
              }}
            />
          ))}
        </g>
      </svg>

      {group.length > 0 && (
        <QuakeCard
          group={group}
          regionName={hoveredRegionName}
          container={stageSize}
          scale={stageSize.width / WIDTH}
        />
      )}
      </div>

      {hiddenCount > 0 && (
        <p className="muted map-note">{hiddenCount} deprem harita çerçevesinin dışında kaldı (komşu bölgeler).</p>
      )}
    </div>
  )
}
