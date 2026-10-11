import type { PlacedQuake } from './earthquakes'

// İç içe giren deprem daireleri bir "küme"dir: haritada tek bir işaret olarak görünür, tıklayınca üyeleri
// etrafa yayılır ("spiderfy"). Tüm hesaplar SVG koordinatındadır (viewBox birimi).

export interface QuakeCluster {
  /** Kararlı kimlik: en büyük üyenin kimliği + üye sayısı (veri yenilenince aynı küme aynı kimliği korur). */
  id: string
  /** Büyüklüğe göre büyükten küçüğe. */
  members: PlacedQuake[]
  /** Üyelerin merkez noktası (ağırlık merkezi). */
  x: number
  y: number
  /** Küme işaretinin yarıçapı. */
  r: number
}

export interface Clustering {
  singles: PlacedQuake[]
  clusters: QuakeCluster[]
}

/** İki daire üst üste biniyor mu? Yalnızca değmek (uzaklık = yarıçaplar toplamı) üst üste binmek sayılmaz. */
export function overlaps(a: PlacedQuake, b: PlacedQuake): boolean {
  return Math.hypot(a.x - b.x, a.y - b.y) < a.r + b.r
}

export function clusterRadius(members: readonly PlacedQuake[]): number {
  return Math.max(10, Math.max(...members.map((m) => m.r)) + 4)
}

/**
 * Zincirleme bağlantılı bileşenler: A-B ve B-C iç içeyse A-C iç içe olmasa da üçü tek kümedir
 * (birleştirme-bulma / union-find). Tek başına kalanlar `singles`, ikiden fazlası olanlar `clusters` olur.
 */
export function clusterQuakes(placed: readonly PlacedQuake[]): Clustering {
  const parent = placed.map((_, i) => i)
  const find = (i: number): number => {
    while (parent[i] !== i) {
      parent[i] = parent[parent[i]] // yol sıkıştırma
      i = parent[i]
    }
    return i
  }

  for (let i = 0; i < placed.length; i++) {
    for (let j = i + 1; j < placed.length; j++) {
      if (overlaps(placed[i], placed[j])) parent[find(i)] = find(j)
    }
  }

  const groups = new Map<number, PlacedQuake[]>()
  placed.forEach((p, i) => {
    const root = find(i)
    const list = groups.get(root)
    if (list) list.push(p)
    else groups.set(root, [p])
  })

  const singles: PlacedQuake[] = []
  const clusters: QuakeCluster[] = []
  for (const group of groups.values()) {
    if (group.length === 1) {
      singles.push(group[0])
      continue
    }
    const members = [...group].sort((a, b) => b.quake.magnitude - a.quake.magnitude)
    clusters.push({
      id: `${members[0].quake.id}+${members.length}`,
      members,
      x: members.reduce((s, m) => s + m.x, 0) / members.length,
      y: members.reduce((s, m) => s + m.y, 0) / members.length,
      r: clusterRadius(members),
    })
  }
  return { singles, clusters }
}

// ---------------- Yayılma (fan) yerleşimi ----------------

export interface FanItem {
  quake: PlacedQuake
  /** Yayılmış dairenin merkezi. */
  x: number
  y: number
}

const fullyInView = (x: number, y: number, r: number, w: number, h: number) =>
  x - r >= 0 && y - r >= 0 && x + r <= w && y + r <= h

/**
 * Küme üyelerini küme merkezinin etrafında halka(lar) üzerine yerleştirir. Komşu daireler birbirine girmez;
 * halka kapasitesi komşular arası KİRİŞ uzunluğundan hesaplanır (yay uzunluğundan değil: az üyede yay yanıltıcı).
 * Üyeler fazlaysa dış halkalar eklenir. Başlangıç açısı, tüm daireler çerçevede kalacak şekilde döndürülür;
 * hepsini sığdıran açı yoksa en çok daireyi sığdıran seçilir. Sonuç deterministiktir.
 */
export function fanLayout(
  center: { x: number; y: number },
  members: readonly PlacedQuake[],
  view: { width: number; height: number },
  gap = 4,
): FanItem[] {
  const sorted = [...members].sort((a, b) => b.quake.magnitude - a.quake.magnitude)
  if (sorted.length === 0) return []
  if (sorted.length === 1) return [{ quake: sorted[0], x: center.x, y: center.y }]

  const rmax = Math.max(...sorted.map((m) => m.r))
  const slot = 2 * rmax + gap // bir dairenin kapladığı yer (çap + boşluk)
  const baseRadius = clusterRadius(sorted) + rmax + 10 // merkez işaretiyle çakışmasın

  // Halkaları doldur: her halkada komşular arası kiriş >= slot olacak kadar üye.
  const rings: { radius: number; count: number }[] = []
  let remaining = sorted.length
  let radius = baseRadius
  while (remaining > 0) {
    const ratio = Math.min(1, slot / (2 * radius))
    const capacity = Math.max(2, Math.floor(Math.PI / Math.asin(ratio)))
    const count = Math.min(capacity, remaining)
    rings.push({ radius, count })
    remaining -= count
    radius += slot + 2
  }

  const place = (offset: number): FanItem[] => {
    const items: FanItem[] = []
    let k = 0
    rings.forEach((ring, ringIndex) => {
      // Dış halkalar yarım adım kaydırılır: iç halkadakilerin hemen arkasına denk gelmesin.
      const start = -Math.PI / 2 + offset + (ringIndex % 2 === 1 ? Math.PI / ring.count : 0)
      for (let i = 0; i < ring.count; i++) {
        const angle = start + (2 * Math.PI * i) / ring.count
        const member = sorted[k++]
        items.push({ quake: member, x: center.x + ring.radius * Math.cos(angle), y: center.y + ring.radius * Math.sin(angle) })
      }
    })
    return items
  }

  const inView = (items: FanItem[]) =>
    items.filter((it) => fullyInView(it.x, it.y, it.quake.r, view.width, view.height)).length

  let best = place(0)
  let bestCount = inView(best)
  for (let step = 1; step < 24 && bestCount < sorted.length; step++) {
    const candidate = place((step * Math.PI * 2) / 24)
    const count = inView(candidate)
    if (count > bestCount) {
      best = candidate
      bestCount = count
    }
  }
  return shiftIntoView(best, view)
}

/**
 * Döndürme yetmezse (küme köşede/kenarda: etrafında tam halka hiçbir açıda sığmaz) tüm yayılmayı çerçevenin
 * içine doğru kaydırır. Halka kümenin merkezinden kayar ama çizgiler her üyeyi kendi gerçek konumuna bağlar.
 * Yayılma çerçeveden büyükse (çok fazla üye) kaydırma yalnızca sol/üstü tutturur.
 */
function shiftIntoView(items: FanItem[], view: { width: number; height: number }): FanItem[] {
  const minX = Math.min(...items.map((i) => i.x - i.quake.r))
  const maxX = Math.max(...items.map((i) => i.x + i.quake.r))
  const minY = Math.min(...items.map((i) => i.y - i.quake.r))
  const maxY = Math.max(...items.map((i) => i.y + i.quake.r))

  const dx = minX < 0 ? -minX : maxX > view.width ? view.width - maxX : 0
  const dy = minY < 0 ? -minY : maxY > view.height ? view.height - maxY : 0
  if (dx === 0 && dy === 0) return items
  return items.map((i) => ({ ...i, x: i.x + dx, y: i.y + dy }))
}

/** Kılavuz çizgisinin daire kenarındaki başlangıç noktası: merkezden hedefe doğru, yarıçap kadar ilerisi. */
export function leaderStart(
  point: { x: number; y: number },
  radius: number,
  target: { x: number; y: number },
): { x: number; y: number } {
  const dx = target.x - point.x
  const dy = target.y - point.y
  const length = Math.hypot(dx, dy) || 1
  return { x: point.x + (dx / length) * radius, y: point.y + (dy / length) * radius }
}
