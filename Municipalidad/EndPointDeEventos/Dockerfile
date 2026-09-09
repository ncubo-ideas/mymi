FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Municipalidad.EventosMock.csproj .
RUN dotnet restore Municipalidad.EventosMock.csproj
COPY . .
RUN dotnet publish Municipalidad.EventosMock.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8121
ENV ASPNETCORE_URLS=http://0.0.0.0:8121
ENTRYPOINT ["dotnet", "Municipalidad.EventosMock.dll"]
