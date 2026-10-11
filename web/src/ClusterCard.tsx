import type { QuakeCluster } from './clusters'
import { CARD_WIDTH, MAX_LISTED, cardFits, cardMinHeight, formatMagnitude, formatWhen, placeCard } from './earthquakes'
import { QuakeLeader, type CardContainer } from './QuakeCard'

interface Props {
  cluster: QuakeCluster
  /** Kümenin ağırlık merkezinin bulunduğu il ("KKTC" dahil); deniz/komşu ülkedeyse null. */
  regionName: string | null
  container: CardContainer
  scale: number
}

/** Küme işaretine gelince açılan özet kartı: kaç deprem, büyüklük aralığı, il ve üyelerin listesi. */
export function ClusterCard({ cluster, regionName, container, scale }: Props) {
  if (container.width === 0) return null

  const n = cluster.members.length
  const point = { x: cluster.x * scale, y: cluster.y * scale }
  const radius = cluster.r * scale
  const minHeight = cardMinHeight(n)
  if (!cardFits(container, { width: CARD_WIDTH, height: minHeight })) return null
  const place = placeCard(point, radius, container, { width: CARD_WIDTH, height: minHeight })

  const magnitudes = cluster.members.map((m) => m.quake.magnitude)
  const hi = Math.max(...magnitudes)
  const lo = Math.min(...magnitudes)
  const listed = cluster.members.slice(0, MAX_LISTED)

  return (
    <>
      <QuakeLeader container={container} from={point} place={place} radius={radius} />

      <div className="quake-card cluster-card" role="tooltip" style={{ left: place.left, top: place.top, width: CARD_WIDTH, minHeight }}>
        <div className="qc-head">
          <strong>{n} deprem</strong>
          <span>M {formatMagnitude(lo)}–{formatMagnitude(hi)}</span>
        </div>
        <dl>
          <dt>İl</dt>
          <dd>{regionName ?? 'Türkiye sınırları dışı (deniz/komşu bölge)'}</dd>
        </dl>
        <div className="qc-group">
          <p>İç içe depremler (büyükten küçüğe):</p>
          <ul>
            {listed.map((m) => (
              <li key={m.quake.id}>
                <strong>M {formatMagnitude(m.quake.magnitude)}</strong> {formatWhen(m.quake.occurredAt)}
              </li>
            ))}
            {n > MAX_LISTED && <li className="muted">+{n - MAX_LISTED} daha</li>}
          </ul>
        </div>
        <p className="qc-hint">Tıklayınca daireler yayılır.</p>
      </div>
    </>
  )
}
