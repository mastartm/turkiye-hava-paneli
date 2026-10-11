# API imajı. Derleme bağlamı: depo kökü (docker-compose.yml bunu ayarlar).
# İki aşamalı: SDK ile derle, yalnızca çalışma zamanı imajına kopyala (küçük ve saldırı yüzeyi dar).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyaları: bağımlılıklar değişmedikçe `restore` katmanı önbellekten gelir.
COPY src/HavaPaneli.Domain/HavaPaneli.Domain.csproj src/HavaPaneli.Domain/
COPY src/HavaPaneli.Application/HavaPaneli.Application.csproj src/HavaPaneli.Application/
COPY src/HavaPaneli.Infrastructure/HavaPaneli.Infrastructure.csproj src/HavaPaneli.Infrastructure/
COPY src/HavaPaneli.Api/HavaPaneli.Api.csproj src/HavaPaneli.Api/
RUN dotnet restore src/HavaPaneli.Api/HavaPaneli.Api.csproj

COPY src/ src/
RUN dotnet publish src/HavaPaneli.Api/HavaPaneli.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# Root olmayan kullanıcı: konteynerden kaçış durumunda zararı sınırlar. (.NET 8 imajlarında "app" kullanıcısı hazırdır.)
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HavaPaneli.Api.dll"]
