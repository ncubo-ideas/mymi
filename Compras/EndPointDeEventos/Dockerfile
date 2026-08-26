# Mock del servidor del proveedor (URL2). Build multi-etapa: el contexto es solo
# esta carpeta, porque el mock no referencia nada del supermercado.
#
# EL PUERTO ES 8101, no 8081 ni 8091 (la trampa que ya mordio a Examenes: este
# archivo se clona entre negocios con su puerto adentro, y ASPNETCORE_URLS le gana
# al "Urls" del appsettings.json). Los puertos por negocio existen para que los
# negocios corran a la vez, que es lo que las historias cruzadas necesitan.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Compras.EventosMock.csproj .
RUN dotnet restore Compras.EventosMock.csproj
COPY . .
RUN dotnet publish Compras.EventosMock.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8101
ENV ASPNETCORE_URLS=http://0.0.0.0:8101
ENTRYPOINT ["dotnet", "EventosMock.dll"]
