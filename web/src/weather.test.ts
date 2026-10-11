import { describe, expect, it } from 'vitest'
import { dailySummary, formatAge, formatClock, isPrecipitationCode, isStale, shortDay, weatherLabel } from './weather'

describe('weatherLabel', () => {
  it('her WMO kodu kendi adını taşır: hafif, orta ve şiddetli ayrı etiketlenir', () => {
    expect(weatherLabel(0)).toBe('Açık')
    expect(weatherLabel(2)).toBe('Parçalı bulutlu')
    expect(weatherLabel(80)).toBe('Hafif sağanak')
    expect(weatherLabel(81)).toBe('Sağanak')
    expect(weatherLabel(82)).toBe('Şiddetli sağanak')
    expect(weatherLabel(61)).toBe('Hafif yağmur')
    expect(weatherLabel(65)).toBe('Şiddetli yağmur')
    expect(weatherLabel(95)).toBe('Gök gürültülü fırtına')
  })

  it('80, 81 ve 82 birbirinden farklı etiketlenir (gerçek denemedeki hata: hepsi "Sağanak" yazıyordu)', () => {
    expect(new Set([weatherLabel(80), weatherLabel(81), weatherLabel(82)]).size).toBe(3)
  })

  it('null veri yok, tanımsız kod bilinmiyor der', () => {
    expect(weatherLabel(null)).toBe('Veri yok')
    expect(weatherLabel(4)).toBe('Bilinmiyor')
  })
})

describe('isPrecipitationCode', () => {
  it('yağış kodlarını tanır, açık/bulutlu/sis kodlarını tanımaz', () => {
    for (const c of [51, 61, 66, 71, 77, 80, 82, 85, 95, 99]) expect(isPrecipitationCode(c)).toBe(true)
    for (const c of [0, 1, 2, 3, 45, 48]) expect(isPrecipitationCode(c)).toBe(false)
    expect(isPrecipitationCode(null)).toBe(false)
  })
})

describe('dailySummary', () => {
  it('gerçek denemedeki durum: 0,4 mm hafif sağanak "kısa süreli" olarak yazılır, tüm gün yağışlı gösterilmez', () => {
    expect(dailySummary(80, 0.4)).toBe('Gün içinde kısa süreli hafif sağanak (0,4 mm)')
  })

  it('1 mm ve üzeri yağışta miktarla birlikte düz etiket', () => {
    expect(dailySummary(81, 6.25)).toBe('Sağanak (6,3 mm)') // 6,25 -> toFixed(1) yarıma yuvarlar: gösterimde bir ondalık
    expect(dailySummary(63, 1)).toBe('Yağmur (1,0 mm)')
  })

  it('yağış olmayan kodda yalnızca etiket (yağış miktarı gösterilmez)', () => {
    expect(dailySummary(2, 0)).toBe('Parçalı bulutlu')
    expect(dailySummary(0, 0.4)).toBe('Açık')
  })

  it('yağış miktarı bilinmiyorsa düz etiket, kod yoksa veri yok', () => {
    expect(dailySummary(80, null)).toBe('Hafif sağanak')
    expect(dailySummary(null, 3)).toBe('Veri yok')
  })

  it('büyük harfle başlayan etiket "kısa süreli" cümlesinde küçük harfe çevrilir (Türkçe karakter bozulmaz)', () => {
    expect(dailySummary(82, 0.5)).toBe('Gün içinde kısa süreli şiddetli sağanak (0,5 mm)')
    expect(dailySummary(95, 0.2)).toBe('Gün içinde kısa süreli gök gürültülü fırtına (0,2 mm)')
  })
})

describe('shortDay', () => {
  it('tarihi gün adı ve gün numarasıyla kısaltır', () => {
    expect(shortDay('2026-10-10')).toBe('Cmt 10') // 10 Ekim 2026 Cumartesi
    expect(shortDay('2026-10-11')).toBe('Paz 11')
  })
})

describe('formatClock', () => {
  it('Türkiye saatiyle (UTC+3) ve gün sınırını aşan saatte doğru', () => {
    expect(formatClock('2026-10-11T10:03:00Z')).toBe('13:03')
    expect(formatClock('2026-10-11T22:30:00+00:00')).toBe('01:30')
  })
})

describe('formatAge / isStale', () => {
  const now = new Date('2026-10-11T12:08:00Z').getTime()

  it('dakika, saat ve gün cinsinden Türkçe yaş', () => {
    expect(formatAge('2026-10-11T12:07:30Z', now)).toBe('az önce')
    expect(formatAge('2026-10-11T11:56:00Z', now)).toBe('12 dk önce')
    expect(formatAge('2026-10-11T10:03:00Z', now)).toBe('2 saat önce') // gerçek denemedeki durum: 13:03'te çekilmiş, şimdi 15:08
    expect(formatAge('2026-10-09T12:00:00Z', now)).toBe('2 gün önce')
  })

  it('gelecekteki zaman (saat farkı) ve geçersiz tarih "az önce" sayılır, çökmez', () => {
    expect(formatAge('2026-10-11T13:00:00Z', now)).toBe('az önce')
    expect(formatAge('bozuk', now)).toBe('az önce')
  })

  it('3 saatten eski veri bayat sayılır; tam sınırda sayılmaz', () => {
    expect(isStale('2026-10-11T09:00:00Z', now)).toBe(true)   // 3 saat 8 dk
    expect(isStale('2026-10-11T09:08:00Z', now)).toBe(false)  // tam 3 saat
    expect(isStale('2026-10-11T12:00:00Z', now)).toBe(false)
  })
})
