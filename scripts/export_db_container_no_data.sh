#!/bin/bash

clear

echo "Exporting the dictionary database without data"

mysqldump -P 3360 --protocol=tcp -uroot -psecret --no-data --single-transaction --skip-lock-tables --databases dictionary > 01_export_no_data.sql 
