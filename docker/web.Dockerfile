# Arayüz imajı: React uygulamasını derler, nginx ile sunar ve /api isteklerini API'ye iletir.
# Derleme bağlamı: depo kökü.
FROM node:22-alpine AS build
WORKDIR /web

# Önce yalnızca bağımlılık dosyaları: değişmedikçe `npm ci` katmanı önbellekten gelir.
COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ ./
# API adresi derleme zamanında boş bırakılır: tarayıcı aynı adrese (/api) gider, nginx API'ye iletir (CORS gerekmez).
RUN npm run build

FROM nginx:alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
