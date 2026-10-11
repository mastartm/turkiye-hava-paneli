import type { Feature, Geometry, Position } from 'geojson'
import type { RegionCollection, RegionProps } from './TurkeyMap'

// Düz (planar) nokta-çokgen testi. d3-geo'nun küresel `geoContains` fonksiyonu çokgenin yön sırasına (winding)
// duyarlıdır ve yanlış yönde tüm dünyayı "içinde" sayabilir; ülke ölçeğinde düz kontrol yeterli ve bu hataya açık değil.

function inRing(lon: number, lat: number, ring: readonly Position[]): boolean {
  let inside = false
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const [xi, yi] = ring[i]
    const [xj, yj] = ring[j]
    if (yi > lat !== yj > lat && lon < ((xj - xi) * (lat - yi)) / (yj - yi) + xi) inside = !inside
  }
  return inside
}

/** Dış halkanın içinde, deliklerin dışında mı. */
function inPolygon(lon: number, lat: number, polygon: readonly (readonly Position[])[]): boolean {
  return inRing(lon, lat, polygon[0]) && !polygon.slice(1).some((hole) => inRing(lon, lat, hole))
}

export function inGeometry(lon: number, lat: number, geometry: Geometry): boolean {
  if (geometry.type === 'Polygon') return inPolygon(lon, lat, geometry.coordinates)
  if (geometry.type === 'MultiPolygon') return geometry.coordinates.some((p) => inPolygon(lon, lat, p))
  return false
}

/**
 * Noktanın hangi bölgenin (il ya da "KKTC") içinde olduğu. Deniz ya da komşu ülkedeyse null.
 * İlçe sınırı verimiz yok; bu yüzden ilçe değil il döner.
 */
export function regionNameAt(regions: RegionCollection, lon: number, lat: number): string | null {
  const hit = regions.features.find((f: Feature<Geometry, RegionProps>) => inGeometry(lon, lat, f.geometry))
  return hit?.properties.name ?? null
}
