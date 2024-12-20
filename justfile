set dotenv-load

version := "1.6"
postgres_container_name := "jellyfish_postgres_container"
addition_args := ""

backup:
    export BACKUP_FILE_NAME="Jellyfish-$(date '+%Y-%m-%d-%H-%M-%S')-dump.tar" && \
    docker exec {{ postgres_container_name }} pg_dump --dbname=jellyfish_kook --file="$BACKUP_FILE_NAME" --username=jellyfish --host=localhost --port=5432 --format=t && \
    mkdir -p DataBackup && cd DataBackup && \
    docker cp {{ postgres_container_name }}:/"$BACKUP_FILE_NAME" .

migrate:
    just backup
    cd ./Jellyfish && dotnet ef database update

docker-deploy:
    # Replace mirror for the aliyun ECS
    sed -i 's|mirrors.aliyun.com|mirrors.cloud.aliyuncs.com|g' ./Jellyfish/Dockerfile
    docker build {{ addition_args }} -f ./Jellyfish/Dockerfile -t $PROJECT_NAME:{{ version }} .
    docker stop $PROJECT_NAME || true
    docker rm $PROJECT_NAME || true
    docker run -d --network=host --name $PROJECT_NAME $PROJECT_NAME:{{ version }}

deploy:
    just docker-deploy

deploy-no-cache:
    just addition_args='--no-cache' docker-deploy
