@echo off

echo [1/7] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/7] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [3/7] Generating JavaScript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsJavaScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error
cd javascript-sdk
echo [4/7] npm install in javascript-sdk...
call npm install || goto :error
cd ..

echo [5/7] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [6/7] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error
cd typescript-sdk
echo [7/7] npm install in typescript-sdk...
call npm install || goto :error
cd ..

echo Succesfully completed
pause

