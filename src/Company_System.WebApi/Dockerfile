FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Company_System.WebApi/Company_System.WebApi.csproj", "src/Company_System.WebApi/"]
COPY ["src/Company_System.Infrastructure/Company_System.Infrastructure.csproj", "src/Company_System.Infrastructure/"]
COPY ["src/Company_System.Core/Company_System.Core.csproj", "src/Company_System.Core/"]
RUN dotnet restore "src/Company_System.WebApi/Company_System.WebApi.csproj"
COPY . .
WORKDIR /src/src/Company_System.WebApi
RUN dotnet build "./Company_System.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Company_System.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Company_System.WebApi.dll"]
