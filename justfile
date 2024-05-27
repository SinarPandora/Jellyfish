set dotenv-load

version := "1.4"
container_name := "jellyfish"
postgres_container_name := "jellyfish_postgres_container"

backup:
    export BACKUP_FILE_NAME="Jellyfish-$(date '+%Y-%m-%d-%H-%M-%S')-dump.tar" && \
    docker exec {{ postgres_container_name }} pg_dump --dbname=jellyfish_kook --file="$BACKUP_FILE_NAME" --username=jellyfish --host=localhost --port=5432 --format=t && \
    mkdir -p DataBackup && cd DataBackup && \
    docker cp {{ postgres_container_name }}:/"$BACKUP_FILE_NAME" .

migrate:
    just backup
    cd ./Jellyfish && dotnet ef database update

deploy:
    docker build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:{{ version }} .
    docker stop {{ container_name }} || true
    docker rm {{ container_name }} || true
    docker run -d --network=host --name {{ container_name }} jellyfish:{{ version }}
