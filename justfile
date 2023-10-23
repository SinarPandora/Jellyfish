set dotenv-load

docker_cli := if os() == "linux" { "sudo docker" } else { "docker" }
container_name := "jellyfish"
postgres_container_name := "jellyfish_postgres_container"

migrate:
    export BACKUP_FILE_NAME="Jellyfish-$(date '+%Y-%m-%d-%H-%M-%S')-dump.tar" && \
    {{ docker_cli }} exec {{ postgres_container_name }} pg_dump --dbname=jellyfish_kook --file="$BACKUP_FILE_NAME" --username=jellyfish --host=localhost --port=5432 && \
    mkdir -p DataBackup && cd DataBackup && \
    {{ docker_cli }} cp "$BACKUP_FILE_NAME" {{ postgres_container_name }}:/
    cd ./Jellyfish && dotnet ef database update

deploy:
    {{ docker_cli }} build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    {{ docker_cli }} stop {{ container_name }} || true
    {{ docker_cli }} rm {{ container_name }} || true
    {{ docker_cli }} run -d --network=host --name {{ container_name }} jellyfish:1.0
