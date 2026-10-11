import { leaderStart } from './clusters'
import {
  CARD_WIDTH,
  cardFits,
  cardMinHeight,
  directionName,
  formatDepth,
  formatMagnitude,
  formatWhen,
  parsePlace,
  placeCard,
  type CardPlacement,
  type PlacedQuake,
} from './earthquakes'

export interface CardContainer {
  width: number
  height: number
}

/** Kartın ve kılavuz çizgisinin ortak yerleşimi: dairenin kenarından kartın kenarına. */
export function QuakeLeader({
  container,
  from,
  place,
  radius,
}: {
  container: CardContainer
  from: { x: number; y: number }
  place: CardPlacement
  radius: number
}) {
  const start = leaderStart(from, radius, { x: place.anchorX, y: place.anchorY })
  return (
    <svg className="quake-leader" width={container.width} height={container.height} aria-hidden="true">
      <line x1={start.x} y1={start.y} x2={place.anchorX} y2={place.anchorY} />
      <circle cx={place.anchorX} cy={place.anchorY} r={3} />
    </svg>
  )
}

interface Props {
  placed: PlacedQuake
  /** Depremin içinde bulunduğu il ("KKTC" dahil); deniz/komşu ülkedeyse null. */
  regionName: string | null
  /** Harita kabının piksel boyutu ve ölçeği (kap genişliği / SVG viewBox genişliği). */
  container: CardContainer
  scale: number
}

const fmtKm = (km: number) => String(km).replace('.', ',')

/** Tek bir depreme gelince açılan ayrıntı kartı ve dairenin kenarından karta uzanan kılavuz çizgisi. */
export function QuakeCard({ placed, regionName, container, scale }: Props) {
  if (container.width === 0) return null

  const q = placed.quake
  const point = { x: placed.x * scale, y: placed.y * scale }
  const radius = placed.r * scale
  const minHeight = cardMinHeight(1)
  if (!cardFits(container, { width: CARD_WIDTH, height: minHeight })) return null
  const place = placeCard(point, radius, container, { width: CARD_WIDTH, height: minHeight })

  const parsed = parsePlace(q.place)

  return (
    <>
      <QuakeLeader container={container} from={point} place={place} radius={radius} />

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
      </div>
    </>
  )
}
