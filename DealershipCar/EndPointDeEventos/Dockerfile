# Mock del servidor del concesionario (URL2). Build multi-etapa: el contexto es solo esta
# carpeta, porque el mock no referencia nada del negocio.
#
# EL PUERTO ES 8092, no el de Examenes ni el de Facturas. Los puertos por negocio existen para
# que los tres mocks corran a la vez, y ASPNETCORE_URLS le gana al "Urls" del appsettings.json:
# si el contenedor escuchara en otro puerto, el mapeo no conectaria con nada.
#
# EL ENTRYPOINT NOMBRA EL DLL QUE ESTE csproj PRODUCE. Suena obvio y no lo es: el Dockerfile de
# la implementacion de referencia arranca "EventosMock.dll" cuando su AssemblyName es
# "Examenes.EventosMock", asi que el contenedor no levanta. Es la landmine que el plan de OS2
# dice arreglar en vez de heredar.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DealershipCar.EventosMock.csproj .
RUN dotnet restore DealershipCar.EventosMock.csproj
COPY . .
RUN dotnet publish DealershipCar.EventosMock.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8092
ENV ASPNETCORE_URLS=http://0.0.0.0:8092
ENTRYPOINT ["dotnet", "DealershipCar.EventosMock.dll"]
