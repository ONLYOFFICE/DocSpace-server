@echo off

echo [1/8] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/8] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [3/8] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [4/8] Generating Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPostmanCollection.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/8] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error


cd generated-code/my-typescript-axios
echo [6/8] npm install in typescript-sdk...
call npm install || goto :error
cd ../..

echo [7/8] Generating Typescript K6 SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsK6TypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error


cd generated-code/my-typescript-k6
echo [8/8] npm install in typescript-k6-sdk...
call npm install || goto :error
cd ../..
echo Succesfully completed
pause

