import {
  CARD_WIDTH,
  MAX_LISTED,
  cardFits,
  cardMinHeight,
  directionName,
  formatDepth,
  formatMagnitude,
  formatWhen,
  parsePlace,
  placeCard,
  type PlacedQuake,
} from './earthquakes'

interface Props {
  /** İlk eleman fare altındaki deprem; geri kalanı onunla üst üste binenler. */
  group: readonly PlacedQuake[]
  /** Depremin içinde bulunduğu il ("KKTC" dahil); deniz/komşu ülkedeyse null. */
  regionName: string | null
  /** Harita kabının piksel boyutu ve ölçeği (kap genişliği / SVG viewBox genişliği). */
  container: { width: number; height: number }
  scale: number
}

const fmtKm = (km: number) => String(km).replace('.', ',')

/** Daireye gelince açılan ayrıntı kartı ve dairenin kenarından karta uzanan kılavuz çizgisi. */
export function QuakeCard({ group, regionName, container, scale }: Props) {
  const main = group[0]
  if (!main || container.width === 0) return null

  const q = main.quake
  const point = { x: main.x * scale, y: main.y * scale }
  const radius = main.r * scale
  const minHeight = cardMinHeight(group.length)
  if (!cardFits(container, { width: CARD_WIDTH, height: minHeight })) return null
  const place = placeCard(point, radius, container, { width: CARD_WIDTH, height: minHeight })

  // Çizgi dairenin merkezinden değil kenarından başlar: daireyi kesmesin.
  const dx = place.anchorX - point.x
  const dy = place.anchorY - point.y
  const length = Math.hypot(dx, dy) || 1
  const start = { x: point.x + (dx / length) * radius, y: point.y + (dy / length) * radius }

  const parsed = parsePlace(q.place)
  const rest = group.slice(1)
  const listed = rest.slice(0, MAX_LISTED)

  return (
    <>
      <svg className="quake-leader" width={container.width} height={container.height} aria-hidden="true">
        <line x1={start.x} y1={start.y} x2={place.anchorX} y2={place.anchorY} />
        <circle cx={place.anchorX} cy={place.anchorY} r={3} />
      </svg>

      <div className="quake-card" role="tooltip" style={{ left: place.left, top: place.top, width: CARD_WIDTH, minHeight }}>
        <div className="qc-head">
          <strong>M {formatMagnitude(q.magnitude)}</strong>
          <span>{formatWhen(q.occurredAt)}</span>
        </div>
        <dl>
          <dt>İl</dt>
          <dd>{regionName ?? 'Türkiye sınırları dışı (deniz/komşu bölge)'}</dd>
          <dt>En yakın yerleşim</dt>
          <dd>{parsed ? parsed.settlement : q.place}</dd>
          {parsed && (
            <>
              <dt>Uzaklık, yön</dt>
              <dd>{fmtKm(parsed.distanceKm)} km, {directionName(parsed.direction)}</dd>
            </>
          )}
          <dt>Derinlik</dt>
          <dd>{formatDepth(q.depthKm)}</dd>
          <dt>Konum</dt>
          <dd>{q.latitude.toFixed(2).replace('.', ',')}° K, {q.longitude.toFixed(2).replace('.', ',')}° D</dd>
        </dl>

        {rest.length > 0 && (
          <div className="qc-group">
            <p>Bu noktada {group.length} deprem iç içe:</p>
            <ul>
              {listed.map((p) => (
                <li key={p.quake.id}>
                  <strong>M {formatMagnitude(p.quake.magnitude)}</strong> {formatWhen(p.quake.occurredAt)}
                </li>
              ))}
              {rest.length > MAX_LISTED && <li className="muted">+{rest.length - MAX_LISTED} daha</li>}
            </ul>
          </div>
        )}
      </div>
    </>
  )
}
