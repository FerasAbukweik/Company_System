FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/HR_System.WebApi/HR_System.WebApi.csproj", "src/HR_System.WebApi/"]
COPY ["src/HR_System.Infrastructure/HR_System.Infrastructure.csproj", "src/HR_System.Infrastructure/"]
COPY ["src/HR_System.Core/HR_System.Core.csproj", "src/HR_System.Core/"]
RUN dotnet restore "src/HR_System.WebApi/HR_System.WebApi.csproj"
COPY . .
WORKDIR /src/src/HR_System.WebApi
RUN dotnet build "./HR_System.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HR_System.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HR_System.WebApi.dll"]
