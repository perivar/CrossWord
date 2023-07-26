#!/bin/bash

clear

docker ps -a

echo "Restarting the scraper container"

docker restart crossword.scraper

docker ps -a
