#!/bin/bash

clear

echo "Importing the dictionary database"

mysql -P 3360 --protocol=tcp -uroot -psecret dictionary < export.backup.sql 
