@echo off

cd "C:\Users\perner\My Projects\CrossWord\CrossWord\bin\Release\net7.0\win10-x64\publish"

CrossWord -i "C:\Users\perner\My Projects\CrossWord\templates\american.txt" ^
-d "C:\Users\perner\My Projects\CrossWord\dict\en" ^
-o "signalr" ^
-p "UMBRELLA"

pause
