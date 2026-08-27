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
