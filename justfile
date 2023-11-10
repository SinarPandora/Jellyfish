set dotenv-load

version := "1.2.0"
container_name := "jellyfish" + "_" + version
postgres_container_name := "jellyfish_postgres_container"

migrate:
    export BACKUP_FILE_NAME="Jellyfish-$(date '+%Y-%m-%d-%H-%M-%S')-dump.tar" && \
    docker exec {{ postgres_container_name }} pg_dump --dbname=jellyfish_kook --file="$BACKUP_FILE_NAME" --username=jellyfish --host=localhost --port=5432 && \
    mkdir -p DataBackup && cd DataBackup && \
    docker cp "$BACKUP_FILE_NAME" {{ postgres_container_name }}:/
    cd ./Jellyfish && dotnet ef database update

deploy:
    docker build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    docker stop {{ container_name }} || true
    docker rm {{ container_name }} || true
    docker run -d --network=host --name {{ container_name }} jellyfish:1.0
