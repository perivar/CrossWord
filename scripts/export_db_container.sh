#!/bin/bash

clear

echo "Exporting the dictionary database"

mysqldump -P 3360 --protocol=tcp -uroot -psecret --databases --single-transaction --skip-lock-tables dictionary > export.sql 
