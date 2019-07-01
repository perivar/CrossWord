#!/bin/bash

clear

docker ps -a

echo "Restarting the scraper container"

docker restart crosswordapi.scraper

docker ps -a
