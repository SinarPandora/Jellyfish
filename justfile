deploy:
    sudo docker build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    sudo docker stop jellyfish
    sudo docker rm jellyfish
    sudo docker run -d --network=host --name jellyfish jellyfish:1.0
