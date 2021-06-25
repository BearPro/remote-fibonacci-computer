FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY . .

WORKDIR "/src/CloudLight"
FROM build AS publish
ARG BUILD_VERSION=1.0.1.0
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app/publish -p:Version=$BUILD_VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir /app/AppData
ENTRYPOINT ["dotnet", "CloudLight.dll"]