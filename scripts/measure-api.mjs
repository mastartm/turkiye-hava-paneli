// API yanıt süresi ölçümü (README'deki tablo bununla üretilir). Bağımlılık yok, yalnızca Node 18+.
// Kullanım:  node scripts/measure-api.mjs http://127.0.0.1:8080
// İki ölçüm yapar: (1) tek tek art arda istek (gecikme), (2) eş zamanlı istek (yük altında gecikme, hata sayısı).
// Sonuçlar ölçümü yapan makineye ve Docker'a bağlıdır; mutlak değil, göreli bir fikir verir.

const base = process.argv[2] ?? 'http://127.0.0.1:8080'
const SEQUENTIAL = 300
const WARMUP = 30
const CONCURRENCY = 20
const CONCURRENT_TOTAL = 600

const endpoints = [
  ['/health', 'sağlık ucu'],
  ['/api/cities', '87 şehir listesi (statik)'],
  ['/api/map', 'harita özeti (87 nokta, veritabanı)'],
  ['/api/cities/ankara/forecast', 'şehir tahmini + anomali'],
  ['/api/earthquakes?days=30&minMagnitude=2.5', 'son 30 gün depremleri'],
]

const percentile = (sorted, p) => sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)]
const fmt = (ms) => ms.toFixed(1)

async function timed(url) {
  const t0 = performance.now()
  const res = await fetch(url)
  await res.arrayBuffer() // gövdeyi tamamen oku: gerçek yanıt süresi
  return { ms: performance.now() - t0, ok: res.ok }
}

function summarize(times) {
  const s = [...times].sort((a, b) => a - b)
  return { p50: percentile(s, 50), p95: percentile(s, 95), p99: percentile(s, 99), max: s[s.length - 1] }
}

console.log(`Hedef: ${base}`)
console.log(`Tek tek: ${SEQUENTIAL} istek (+${WARMUP} ısınma) | Eş zamanlı: ${CONCURRENT_TOTAL} istek, ${CONCURRENCY} paralel\n`)

console.log('| Uç | p50 | p95 | p99 | maks | hata |')
console.log('|---|---:|---:|---:|---:|---:|')
for (const [path, label] of endpoints) {
  for (let i = 0; i < WARMUP; i++) await timed(base + path)
  const times = []
  let errors = 0
  for (let i = 0; i < SEQUENTIAL; i++) {
    const r = await timed(base + path)
    times.push(r.ms)
    if (!r.ok) errors++
  }
  const s = summarize(times)
  console.log(`| \`${path}\` (${label}) | ${fmt(s.p50)} ms | ${fmt(s.p95)} ms | ${fmt(s.p99)} ms | ${fmt(s.max)} ms | ${errors} |`)
}

console.log('\nEş zamanlı (yük altında):\n')
console.log('| Uç | istek/sn | p50 | p95 | maks | hata |')
console.log('|---|---:|---:|---:|---:|---:|')
for (const [path, label] of endpoints.slice(1)) {
  const times = []
  let errors = 0
  let next = 0
  const t0 = performance.now()
  await Promise.all(
    Array.from({ length: CONCURRENCY }, async () => {
      while (next++ < CONCURRENT_TOTAL) {
        const r = await timed(base + path)
        times.push(r.ms)
        if (!r.ok) errors++
      }
    }),
  )
  const seconds = (performance.now() - t0) / 1000
  const s = summarize(times)
  console.log(`| \`${path}\` (${label}) | ${(times.length / seconds).toFixed(0)} | ${fmt(s.p50)} ms | ${fmt(s.p95)} ms | ${fmt(s.max)} ms | ${errors} |`)
}
