FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY source/*.sln ./
COPY source/Volo.Opcua.Server/*.csproj ./Volo.Opcua.Server/
COPY source/Volo.Opcua.Server.Api/*.csproj ./Volo.Opcua.Server.Api/

RUN dotnet restore
COPY source/. .

WORKDIR /src/Volo.Opcua.Server.Api
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app

COPY --from=build /src/Volo.Opcua.Server.Api/out ./

EXPOSE 7718

ENTRYPOINT ["dotnet", "Volo.Opcua.Server.Api.dll", "--settings", "config/appsettings.json"]