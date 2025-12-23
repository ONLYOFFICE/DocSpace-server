@echo off

echo [1/12] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/12] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error
cd ../../../sdk/docspace-api-sdk-csharp

echo [3/12] dotnet build install in csharp-sdk...
call dotnet build || goto :error
xcopy "src\DocSpace.API.SDK\bin\Debug\*.nupkg" "..\..\.nuget\packages" /s /y /b /i
cd ../../common/Tools/CustomGenerators

echo [4/12] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [5/12] Generating Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPostmanCollection.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [6/12] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error
cd ../../../sdk/docspace-api-sdk-typescript

echo [7/12] npm install in typescript-sdk...
call npm install || goto :error
cd ../../common/Tools/CustomGenerators

echo [8/12] Generating Java SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsJava.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [9/12] Generating Kotlin SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsKotlin.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [10/12] Generating PHP SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPhp.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

echo [11/12] Generating Swift6 SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsSwift6.json --custom-generator target/custom-generators-1.0-SNAPSHOT-jar-with-dependencies.jar || goto :error

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

