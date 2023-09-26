deploy:
    sudo docker build --no-cache -f ./Jellyfish/Dockerfile -t jellyfish:1.0 .
    sudo docker stop jellyfish || true
    sudo docker rm jellyfish || true
    sudo docker run -d --network=host --name jellyfish jellyfish:1.0
