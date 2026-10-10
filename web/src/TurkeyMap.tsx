import { geoMercator, geoPath } from 'd3-geo'
import type { Feature, FeatureCollection, Geometry } from 'geojson'
import { useMemo, useState } from 'react'
import { colorFor, formatAnomaly } from './anomaly'

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
}

const WIDTH = 960
const HEIGHT = 440

/**
 * Türkiye (81 il) + KKTC haritası, GeoJSON'dan doğrudan SVG olarak çizilir.
 * Renk: bölgenin normalden sapması. Üzerine gelince bölge yükselir, tıklayınca seçilir.
 */
export function TurkeyMap({ regions, anomalies, selectedId, onSelect }: Props) {
  const [hoveredId, setHoveredId] = useState<string | null>(null)

  // Projeksiyon ve yol hesabı pahalı: yalnızca sınır verisi değişince yeniden hesaplanır.
  const paths = useMemo(() => {
    const projection = geoMercator().fitExtent(
      [
        [8, 8],
        [WIDTH - 8, HEIGHT - 8],
      ],
      regions,
    )
    const toPath = geoPath(projection)
    return regions.features.map((f: Feature<Geometry, RegionProps>) => ({ props: f.properties, d: toPath(f) ?? '' }))
  }, [regions])

  const hovered = paths.find((p) => p.props.id === hoveredId) ?? null

  const label = (id: string, name: string) => `${name}, sapma ${formatAnomaly(anomalies.get(id) ?? null)}`

  return (
    <div className="map-wrap">
      <p className="map-caption" aria-live="polite">
        {hovered ? (
          <>
            <strong>{hovered.props.name}</strong> · sapma {formatAnomaly(anomalies.get(hovered.props.id) ?? null)}
          </>
        ) : (
          'Bir ile gelin ya da tıklayın'
        )}
      </p>

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
      </svg>
    </div>
  )
}
