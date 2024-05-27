FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y chromium fonts-noto-cjk
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Jellyfish/Jellyfish.csproj", "Jellyfish/"]
RUN dotnet restore "Jellyfish/Jellyfish.csproj"
COPY . .
WORKDIR "/src/Jellyfish"
RUN dotnet build "Jellyfish.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jellyfish.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jellyfish.dll"]
