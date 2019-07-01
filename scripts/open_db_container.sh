#!/bin/bash

clear

echo "Entering the dictionary database"

docker exec -it crosswordapi.db mysql -uroot -psecret
#mysql -P 3360 --protocol=tcp -uroot -psecret
