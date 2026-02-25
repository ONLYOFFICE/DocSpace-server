@echo off

echo [1/2] Installing openapi-generator-cli globally via npm...
call npm install -g @openapitools/openapi-generator-cli

echo [2/2] Installing Maven
call choco install maven

echo Done