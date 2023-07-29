#!/bin/bash

clear

echo "Exporting the dictionary database with init data"

echo "USE dictionary;" > 02_export_init_data.sql
mysqldump -P 3360 --protocol=tcp -uroot -psecret --no-create-info --databases dictionary --tables AspNetUserClaims AspNetUsers DictionaryUsers __EFMigrationsHistory >> 02_export_init_data.sql
