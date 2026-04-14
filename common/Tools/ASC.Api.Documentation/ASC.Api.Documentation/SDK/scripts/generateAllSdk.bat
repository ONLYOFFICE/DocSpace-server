@echo off

echo [1/14] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/14] Generating C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsCSharp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd ../../../../../sdk/docspace-api-sdk-csharp

echo [3/14] dotnet build install in csharp-sdk...
call dotnet build || goto :error
xcopy "src\DocSpace.API.SDK\bin\Debug\*.nupkg" "..\..\.nuget\packages" /s /y /b /i
cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK

echo [4/14] Generating Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPython.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [5/14] Generating Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPostmanCollection.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [6/14] Generating Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsTypeScript.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd ../../../../../sdk/docspace-api-sdk-typescript

echo [7/14] npm install in typescript-sdk...
call npm install || goto :error
call npm pack || goto :error
xcopy "onlyoffice-docspace-api-sdk-*.tgz" "..\..\..\client\libs\ui-kit"  /s /y /b /i
cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK

echo [8/14] Generating Java SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsJava.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [9/14] Generating Kotlin SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsKotlin.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [10/14] Generating PHP SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsPhp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [11/14] Generating Swift6 SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsSwift6.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [12/14] Generating Go SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsGo.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [13/14] Generating Ruby SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/toolsRuby.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

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

