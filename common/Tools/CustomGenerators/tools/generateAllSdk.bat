@echo off

echo [1/7] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/5] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [3/5] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [4/5] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error
cd typescript-sdk
echo [5/5] npm install in typescript-sdk...
call npm install || goto :error
cd ..

echo Succesfully completed
pause

