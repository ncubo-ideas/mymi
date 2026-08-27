# Mock del servidor del proveedor (URL2). Build multi-etapa: el contexto es solo
# esta carpeta, porque el mock no referencia nada del lab.
#
# El csproj lleva el prefijo del negocio, igual que en Facturas: es lo que permite
# distinguirlos cuando los dos estan abiertos en la misma solucion.
#
# EL PUERTO ES 8111, no 8081. Este archivo venia clonado de Facturas con su puerto
# adentro, y ASPNETCORE_URLS le gana al "Urls" del appsettings.json: el contenedor
# escuchaba en 8081 mientras el compose publicaba 8111:8111, asi que el mapeo no
# conectaba con nada. Los puertos por negocio existen para que Facturas y Cargas
# corran a la vez, y ese es justo el caso en que esto muerde.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Cargas.EventosMock.csproj .
RUN dotnet restore Cargas.EventosMock.csproj
COPY . .
RUN dotnet publish Cargas.EventosMock.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8111
ENV ASPNETCORE_URLS=http://0.0.0.0:8111
ENTRYPOINT ["dotnet", "EventosMock.dll"]
