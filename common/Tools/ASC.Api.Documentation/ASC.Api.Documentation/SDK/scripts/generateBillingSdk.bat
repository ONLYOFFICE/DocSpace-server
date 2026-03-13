@echo off

echo [1/10] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/10] Generating Billing C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsCSharp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd generated-sdk/billing-api-sdk-csharp

echo [3/10] dotnet build install in csharp-sdk...
call dotnet build || goto :error
cd ../../

echo [4/10] Generating Billing Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsPython.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [5/10] Generating Billing Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsPostmanCollection.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [6/10] Generating Billing Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsTypeScript.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd generated-sdk/billing-api-sdk-typescript

echo [7/10] npm install in typescript-sdk...
call npm install || goto :error
call npm pack || goto :error
cd ../../

echo [8/10] Generating Billing Java SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsJava.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [10/10] Generating Billing PHP SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/Billing/toolsPhp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo Succesfully completed
goto :end

:error
echo.
echo ========================================
echo ERROR: Build failed at step above!
echo Error code: %errorlevel%
echo ========================================
pause
exit /b 1

:end
pause
exit /b 0

