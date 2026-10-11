import { describe, expect, it } from 'vitest'
import { clusterQuakes, clusterRadius, fanLayout, leaderStart, overlaps } from './clusters'
import type { PlacedQuake } from './earthquakes'

function p(id: string, x: number, y: number, r = 8, magnitude = 4): PlacedQuake {
  return { quake: { id, occurredAt: '2026-10-06T23:50:00Z', magnitude, place: 'X', latitude: 38, longitude: 36, depthKm: 10 }, x, y, r }
}

describe('overlaps', () => {
  it('yarıçaplar toplamından yakınsa üst üste biner', () => {
    expect(overlaps(p('a', 0, 0, 10), p('b', 19, 0, 10))).toBe(true)
  })

  it('yalnızca değmek (uzaklık = yarıçaplar toplamı) üst üste binmek değildir', () => {
    expect(overlaps(p('a', 0, 0, 10), p('b', 20, 0, 10))).toBe(false)
    expect(overlaps(p('a', 0, 0, 10), p('b', 50, 0, 10))).toBe(false)
  })
})

describe('clusterQuakes', () => {
  it('iç içe olmayanlar tek kalır, küme oluşmaz', () => {
    const r = clusterQuakes([p('a', 0, 0), p('b', 100, 0), p('c', 0, 100)])

    expect(r.clusters).toHaveLength(0)
    expect(r.singles).toHaveLength(3)
  })

  it('iç içe olanlar tek kümede toplanır, geri kalanlar tek kalır', () => {
    const r = clusterQuakes([p('a', 0, 0), p('b', 5, 0), p('far', 300, 300)])

    expect(r.clusters).toHaveLength(1)
    expect(r.clusters[0].members.map((m) => m.quake.id).sort()).toEqual(['a', 'b'])
    expect(r.singles.map((s) => s.quake.id)).toEqual(['far'])
  })

  it('zincirleme: A-B ve B-C iç içeyse A-C iç içe olmasa da üçü tek kümedir', () => {
    // A(0) ve C(28): uzaklık 28 > 16 (iç içe değil); B(14) ikisiyle de iç içe.
    const r = clusterQuakes([p('a', 0, 0, 8), p('b', 14, 0, 8), p('c', 28, 0, 8)])

    expect(overlaps(p('a', 0, 0, 8), p('c', 28, 0, 8))).toBe(false)
    expect(r.clusters).toHaveLength(1)
    expect(r.clusters[0].members).toHaveLength(3)
  })

  it('iki ayrı küme birbirine karışmaz', () => {
    const r = clusterQuakes([p('a', 0, 0), p('b', 4, 0), p('c', 500, 0), p('d', 504, 0), p('e', 508, 0)])

    expect(r.clusters.map((c) => c.members.length).sort()).toEqual([2, 3])
  })

  it('üyeler büyüklüğe göre büyükten küçüğe, merkez ağırlık merkezi, kimlik en büyük üye + sayı', () => {
    const r = clusterQuakes([p('small', 0, 0, 8, 3), p('big', 10, 20, 8, 5), p('mid', 0, 10, 8, 4)])
    const c = r.clusters[0]

    expect(c.members.map((m) => m.quake.id)).toEqual(['big', 'mid', 'small'])
    expect(c.x).toBeCloseTo(10 / 3, 6)
    expect(c.y).toBeCloseTo(10, 6)
    expect(c.id).toBe('big+3')
  })

  it('girdi sırası sonucu değiştirmez (kimlik kararlı)', () => {
    const a = clusterQuakes([p('x', 0, 0, 8, 3), p('y', 4, 0, 8, 5)]).clusters[0].id
    const b = clusterQuakes([p('y', 4, 0, 8, 5), p('x', 0, 0, 8, 3)]).clusters[0].id

    expect(a).toBe(b)
  })

  it('boş girdi boş sonuç verir', () => {
    expect(clusterQuakes([])).toEqual({ singles: [], clusters: [] })
  })

  it('küme işaretinin yarıçapı en büyük üyeden büyüktür, en az 10', () => {
    expect(clusterRadius([p('a', 0, 0, 3), p('b', 0, 0, 4)])).toBe(10)
    expect(clusterRadius([p('a', 0, 0, 12), p('b', 0, 0, 4)])).toBe(16)
  })
})

describe('fanLayout', () => {
  const view = { width: 960, height: 440 }
  const center = { x: 480, y: 220 }

  const members = (n: number) => Array.from({ length: n }, (_, i) => p(`m${i}`, 480, 220, 6 + (i % 4), 5 - i * 0.01))

  const minGap = (items: ReturnType<typeof fanLayout>) => {
    let min = Infinity
    for (let i = 0; i < items.length; i++)
      for (let j = i + 1; j < items.length; j++)
        min = Math.min(min, Math.hypot(items[i].x - items[j].x, items[i].y - items[j].y) - (items[i].quake.r + items[j].quake.r))
    return min
  }

  it('her üye için bir konum üretir, hiçbiri küme merkezinde kalmaz', () => {
    const items = fanLayout(center, members(6), view)

    expect(items).toHaveLength(6)
    for (const it of items) expect(Math.hypot(it.x - center.x, it.y - center.y)).toBeGreaterThan(20)
  })

  it.each([2, 3, 6, 12, 20, 40])('%i üyede yayılan daireler birbirine girmez', (n) => {
    expect(minGap(fanLayout(center, members(n), view))).toBeGreaterThanOrEqual(0)
  })

  it('merkez rahatken en büyük üye tepede (saat 12) durur', () => {
    const items = fanLayout(center, members(5), view)

    expect(items[0].quake.quake.id).toBe('m0')
    expect(items[0].x).toBeCloseTo(center.x, 3)
    expect(items[0].y).toBeLessThan(center.y)
  })

  it('merkez köşeye yakınken de tüm daireler çerçevede kalacak şekilde döner', () => {
    for (const c of [{ x: 12, y: 12 }, { x: 948, y: 12 }, { x: 12, y: 428 }, { x: 948, y: 428 }]) {
      const items = fanLayout(c, members(4), view)
      for (const it of items) {
        expect(it.x - it.quake.r).toBeGreaterThanOrEqual(0)
        expect(it.y - it.quake.r).toBeGreaterThanOrEqual(0)
        expect(it.x + it.quake.r).toBeLessThanOrEqual(view.width)
        expect(it.y + it.quake.r).toBeLessThanOrEqual(view.height)
      }
    }
  })

  it('çok üyede dış halka eklenir ve iç halkadan uzaktadır', () => {
    const items = fanLayout(center, members(40), view)
    const radii = items.map((it) => Math.hypot(it.x - center.x, it.y - center.y))

    expect(Math.max(...radii)).toBeGreaterThan(Math.min(...radii) + 10)
  })

  it('deterministiktir: aynı girdi aynı konumları verir', () => {
    expect(fanLayout(center, members(7), view)).toEqual(fanLayout(center, members(7), view))
  })

  it('tek üye merkezde kalır, boş liste boş döner', () => {
    expect(fanLayout(center, [p('a', 480, 220)], view)).toEqual([{ quake: p('a', 480, 220), x: 480, y: 220 }])
    expect(fanLayout(center, [], view)).toEqual([])
  })

  it('girdi dizisini değiştirmez', () => {
    const input = members(4)
    const before = input.map((m) => m.quake.id)
    fanLayout(center, input, view)

    expect(input.map((m) => m.quake.id)).toEqual(before)
  })
})

describe('leaderStart', () => {
  it('çizgi dairenin kenarından, hedefe doğru başlar', () => {
    const s = leaderStart({ x: 0, y: 0 }, 10, { x: 100, y: 0 })

    expect(s.x).toBeCloseTo(10, 6)
    expect(s.y).toBeCloseTo(0, 6)
  })

  it('çapraz yönde de yarıçap kadar uzaklıkta', () => {
    const s = leaderStart({ x: 5, y: 5 }, 10, { x: 105, y: 105 })

    expect(Math.hypot(s.x - 5, s.y - 5)).toBeCloseTo(10, 6)
  })

  it('hedef merkezle aynıysa sıfıra bölmez', () => {
    const s = leaderStart({ x: 3, y: 4 }, 10, { x: 3, y: 4 })

    expect(Number.isFinite(s.x) && Number.isFinite(s.y)).toBe(true)
  })
})
