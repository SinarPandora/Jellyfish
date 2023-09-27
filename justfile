migrate:
    mkdir -p DataBackup && cd DataBackup
    pg_dump --dbname=jellyfish_kook --file="DataBackup/Postgres-$(date '+%Y-%m-%d-%H-%M-%S')-dump.sql" --username=jellyfish --host=localhost --port=5432
    cd ./Jellyfish && dotnet ef database update

[linux]
deploy:
    sudo docker build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    sudo docker stop jellyfish || true
    sudo docker rm jellyfish || true
    sudo docker run -d --network=host --name jellyfish jellyfish:1.0

[macos]
deploy:
    podman build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    podman stop jellyfish || true
    podman rm jellyfish || true
    podman run -d --network=host --name jellyfish jellyfish:1.0
