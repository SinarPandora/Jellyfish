FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone && \
    add-apt-repository ppa:xtradeb/apps -y && \
    apt-get update && apt-get install -y chromium fonts-noto-cjk wget && \
    useradd -ms /bin/bash jellyfish
USER jellyfish
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
USER jellyfish
ENTRYPOINT ["dotnet", "Jellyfish.dll"]
