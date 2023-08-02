@echo off

cd "C:\Users\perner\My Projects\CrossWord\CrossWord\bin\Release\net7.0\win10-x64\publish"

CrossWord -i "C:\Users\perner\My Projects\CrossWord\templates\template1.txt" ^
-o "database" ^
-d "C:\Users\perner\My Projects\CrossWord\dict\norwegian-synonyms.json"

pause
