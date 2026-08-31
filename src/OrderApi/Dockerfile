FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/OrderApi/OrderApi.csproj src/OrderApi/
RUN dotnet restore src/OrderApi/OrderApi.csproj

COPY src/OrderApi/ src/OrderApi/
RUN dotnet publish src/OrderApi/OrderApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderApi.dll"]
