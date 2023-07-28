#!/bin/bash

clear

echo "Exporting the dictionary database without data"

mysqldump -P 3360 --protocol=tcp -uroot -psecret --no-data --databases --single-transaction --skip-lock-tables dictionary > export_no_data.sql 
