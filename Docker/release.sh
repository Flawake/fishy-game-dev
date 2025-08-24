#!/usr/bin/env bash

docker build -t ghcr.io/flawake/fishy-game-server-linux:latest .
docker push ghcr.io/flawake/fishy-game-server-linux:latest

