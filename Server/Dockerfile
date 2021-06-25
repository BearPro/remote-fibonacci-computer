FROM mcr.microsoft.com/dotnet/sdk:5.0 as publish
WORKDIR /src
COPY . .
WORKDIR /src/Server
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
RUN mkdir /app/AppData
ENTRYPOINT ["dotnet", "Server.dll"]