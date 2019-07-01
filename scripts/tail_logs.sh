#!/bin/bash

clear

echo "Tailing logs"

cd "CrossWord/CrossWord.Scraper"
docker-compose logs -f --tail="100"


