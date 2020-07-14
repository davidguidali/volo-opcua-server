FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY *.sln ./
COPY Volo.Opcua.Server.Core/*.csproj ./Volo.Opcua.Server.Core/
COPY Volo.Opcua.Server.Api/*.csproj ./Volo.Opcua.Server.Api/

RUN dotnet restore
COPY . .

WORKDIR /src/Volo.Opcua.Server.Api
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS runtime
WORKDIR /app

COPY --from=build /src/Volo.Opcua.Server.Api/out ./

EXPOSE 50051
EXPOSE 7718

ENTRYPOINT ["dotnet", "Volo.Opcua.Server.Api.dll"]