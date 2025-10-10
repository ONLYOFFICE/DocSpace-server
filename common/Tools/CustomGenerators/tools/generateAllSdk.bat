@echo off

echo [1/6] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/6] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [3/6] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [4/6] Generating Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPostmanCollection.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/6] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error


cd typescript-sdk
echo [6/6] npm install in typescript-sdk...
call npm install || goto :error
cd ..

echo [7/6] Generating Typescript SDK K6 with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsK6TypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error


cd typescript-k6-sdk
echo [8/6] npm install in typescript-k6-sdk...
call npm install || goto :error
cd ..

echo [5/6] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsJava.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/6] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsKotlin.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/6] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPhp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/6] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsSwift6.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo Succesfully completed
pause

