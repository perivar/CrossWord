#!/bin/bash

clear

echo "Exporting the dictionary database without data"

mysqldump -P 3360 --protocol=tcp -uroot -psecret --no-data --compact --databases dictionary > export_no_data.sql 
