FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY source/Volo.Opcua.Server/*.csproj ./
COPY source/Volo.Opcua.Server/appsettings.json ./

RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM  mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 7718

ENTRYPOINT ["dotnet", "Volo.Opcua.Server.dll"]