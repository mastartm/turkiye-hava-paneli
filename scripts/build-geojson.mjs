// Natural Earth (kamu malı) 10m admin-1 dosyasından Türkiye (81 il) + KKTC poligonlarını ayıklar, basitleştirir
// ve CityCatalog.cs koordinatlarıyla çapraz doğrular. Kullanım:
//   node scripts/build-geojson.mjs <ne_10m_admin_1_states_provinces.geojson> src/HavaPaneli.Domain/CityCatalog.cs web/public/data/turkiye-kktc.geojson
// Kaynak dosya (40,7 MB) repoya girmez: https://github.com/nvkelso/natural-earth-vector (geojson/ klasörü).
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";

const [, , neFile, catalogFile, outFile] = process.argv;

// --- Katalog: CityCatalog.cs'ten (T = Türkiye, K = KKTC) ---
const src = readFileSync(catalogFile, "utf8");
const cities = [...src.matchAll(/([TK])\("([^"]+)",\s*([\d.]+),\s*([\d.]+)\)/g)]
  .map(m => ({ country: m[1], name: m[2], lat: +m[3], lon: +m[4] }));

const slug = (n) => [...n.trim()].map(ch => ({ ç:"c",Ç:"c",ğ:"g",Ğ:"g",ı:"i",İ:"i",I:"i",i:"i",ö:"o",Ö:"o",ş:"s",Ş:"s",ü:"u",Ü:"u",â:"a",Â:"a",î:"i",Î:"i",û:"u",Û:"u"," ":"-" }[ch] ?? ch.toLowerCase())).join("");

// --- Geometri yardımcıları ---
const polys = (g) => g.type === "Polygon" ? [g.coordinates] : g.coordinates;
function inRing(pt, ring) {
  let inside = false;
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const [xi, yi] = ring[i], [xj, yj] = ring[j];
    if ((yi > pt[1]) !== (yj > pt[1]) && pt[0] < ((xj - xi) * (pt[1] - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}
const inPoly = (pt, poly) => inRing(pt, poly[0]) && !poly.slice(1).some(h => inRing(pt, h));
const inGeom = (pt, g) => polys(g).some(p => inPoly(pt, p));

const kmDist = (a, b) => { const dx = (a[0] - b[0]) * 85, dy = (a[1] - b[1]) * 111; return Math.hypot(dx, dy); };
const nearestKm = (pt, g) => Math.min(...polys(g).flatMap(po => po.flatMap(r => r.map(v => kmDist(pt, v)))));

// Douglas-Peucker (yinelemesiz), tolerans derece cinsinden (~0,005° ≈ 500 m)
function dp(points, tol) {
  const keep = new Uint8Array(points.length);
  keep[0] = keep[points.length - 1] = 1;
  const stack = [[0, points.length - 1]];
  while (stack.length) {
    const [s, e] = stack.pop();
    let maxD = 0, idx = -1;
    const [x1, y1] = points[s], [x2, y2] = points[e];
    const dx = x2 - x1, dy = y2 - y1, len = Math.hypot(dx, dy);
    for (let i = s + 1; i < e; i++) {
      const [x, y] = points[i];
      const d = len === 0 ? Math.hypot(x - x1, y - y1) : Math.abs(dy * x - dx * y + x2 * y1 - y2 * x1) / len;
      if (d > maxD) { maxD = d; idx = i; }
    }
    if (maxD > tol && idx > 0) { keep[idx] = 1; stack.push([s, idx], [idx, e]); }
  }
  return points.filter((_, i) => keep[i]);
}
const round = (c) => [Math.round(c[0] * 1000) / 1000, Math.round(c[1] * 1000) / 1000];
const area = (ring) => Math.abs(ring.reduce((a, p, i) => { const q = ring[(i + 1) % ring.length]; return a + (p[0] * q[1] - q[0] * p[1]); }, 0) / 2);

function simplify(g, tol) {
  const outPolys = [];
  for (const poly of polys(g)) {
    const rings = poly
      .map((r, i) => ({ r: dp(r, tol).map(round), i }))
      .filter(({ r, i }) => r.length >= 4 && (i === 0 || area(r) > 1e-4)); // minik delikleri at
    if (rings.length && rings[0].i === 0 && area(rings[0].r) > 1e-4) outPolys.push(rings.map(x => x.r)); // minik adacıkları at
  }
  return outPolys.length === 1 ? { type: "Polygon", coordinates: outPolys[0] } : { type: "MultiPolygon", coordinates: outPolys };
}

// --- Natural Earth ---
const ne = JSON.parse(readFileSync(neFile, "utf8"));
const turFeatures = ne.features.filter(f => f.properties.adm0_a3 === "TUR");
const kktcFeature = ne.features.find(f => f.properties.adm0_a3 === "CYN");
if (!kktcFeature) throw new Error("KKTC (CYN) özelliği bulunamadı");

const trCities = cities.filter(c => c.country === "T");
let problems = 0;
const out = [];
const claimed = new Map();

for (const f of turFeatures) {
  const hits = trCities.filter(c => inGeom([c.lon, c.lat], f.geometry));
  if (hits.length !== 1) { problems++; console.log(`SORUN: ${f.properties.name} içinde ${hits.length} şehir: ${hits.map(h => h.name).join(", ")}`); continue; }
  const city = hits[0];
  if (claimed.has(city.name)) { problems++; console.log(`SORUN: ${city.name} iki ile eşleşti (${claimed.get(city.name)}, ${f.properties.name})`); continue; }
  claimed.set(city.name, f.properties.name);
  out.push({ type: "Feature", properties: { id: slug(city.name), name: city.name, country: "Turkiye" }, geometry: simplify(f.geometry, 0.005) });
}
const unmatchedFeatures = turFeatures.filter(f => !trCities.some(c => claimed.get(c.name) === f.properties.name));
const unmatchedCities = trCities.filter(c => !claimed.has(c.name));
if (unmatchedFeatures.length === 1 && unmatchedCities.length === 1) {
  const f = unmatchedFeatures[0], c = unmatchedCities[0];
  const d = nearestKm([c.lon, c.lat], f.geometry);
  console.log(`Yedek eşleme: ${c.name} ↔ ${f.properties.name}, poligona uzaklık ${d.toFixed(2)} km`);
  if (d < 5) { claimed.set(c.name, f.properties.name); problems--; out.push({ type: "Feature", properties: { id: slug(c.name), name: c.name, country: "Turkiye" }, geometry: simplify(f.geometry, 0.005) }); }
}
const unmatched = trCities.filter(c => !claimed.has(c.name));
if (unmatched.length) { problems += unmatched.length; console.log("Eşleşmeyen katalog şehirleri:", unmatched.map(c => c.name).join(", ")); }

out.push({ type: "Feature", properties: { id: "kktc", name: "KKTC", country: "Kktc" }, geometry: simplify(kktcFeature.geometry, 0.003) });

const kktcCities = cities.filter(c => c.country === "K");
const inKktc = kktcCities.filter(c => inGeom([c.lon, c.lat], kktcFeature.geometry));
for (const c of kktcCities.filter(c => !inKktc.includes(c))) console.log(`KKTC dışı nokta ${c.name}: poligona uzaklık ${nearestKm([c.lon, c.lat], kktcFeature.geometry).toFixed(2)} km`);
console.log(`KKTC poligonu içinde kalan ilçe noktası: ${inKktc.length}/${kktcCities.length}`, kktcCities.filter(c => !inKktc.includes(c)).map(c => `(dışarıda: ${c.name})`).join(" "));

console.log(`\nTürkiye ili eşleşen: ${claimed.size}/81 | sorun: ${problems}`);
const fc = { type: "FeatureCollection", features: out };
const json = JSON.stringify(fc);
mkdirSync(dirname(outFile), { recursive: true });
writeFileSync(outFile, json);
console.log(`yazıldı: ${outFile} | ${out.length} özellik | ${(json.length / 1024).toFixed(0)} KB`);
