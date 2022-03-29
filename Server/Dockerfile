FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /src
COPY "BuildOverrideService.csproj" .
RUN dotnet restore BuildOverrideService.csproj
COPY . .
WORKDIR "/src"
RUN dotnet build "BuildOverrideService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuildOverrideService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BuildOverrideService.dll"]
