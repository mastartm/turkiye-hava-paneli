# Toplayıcı imajı ("çalış ve bitir" programı; docs/ARCHITECTURE.md K-015). Derleme bağlamı: depo kökü.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HavaPaneli.Domain/HavaPaneli.Domain.csproj src/HavaPaneli.Domain/
COPY src/HavaPaneli.Application/HavaPaneli.Application.csproj src/HavaPaneli.Application/
COPY src/HavaPaneli.Infrastructure/HavaPaneli.Infrastructure.csproj src/HavaPaneli.Infrastructure/
COPY src/HavaPaneli.Collector/HavaPaneli.Collector.csproj src/HavaPaneli.Collector/
RUN dotnet restore src/HavaPaneli.Collector/HavaPaneli.Collector.csproj

COPY src/ src/
RUN dotnet publish src/HavaPaneli.Collector/HavaPaneli.Collector.csproj -c Release -o /out --no-restore

# Web uygulaması değil: ASP.NET çalışma zamanı gerekmez, daha küçük "runtime" imajı yeter.
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /out .

# Hesaplanmış iklim normalleri (data/climate-normals.json): yeni kurulum Open-Meteo'ya gitmeden yükler (K-025).
COPY data/ data/

USER app
ENTRYPOINT ["dotnet", "HavaPaneli.Collector.dll"]
