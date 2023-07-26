#!/bin/bash

clear

echo "Entering the scraper container"

docker exec --user root -it crossword.scraper bash

