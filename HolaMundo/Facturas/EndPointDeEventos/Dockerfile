# Mock del servidor del proveedor (URL2). Build multi-etapa: el contexto es solo
# esta carpeta, porque el mock no referencia nada del lab.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Facturas.EventosMock.csproj .
RUN dotnet restore Facturas.EventosMock.csproj
COPY . .
RUN dotnet publish Facturas.EventosMock.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8081
ENV ASPNETCORE_URLS=http://0.0.0.0:8081
ENTRYPOINT ["dotnet", "EventosMock.dll"]
