@echo off

echo [1/11] Building custom generator with Maven...
cd ..
call mvn clean package || goto :error
echo Maven build done.

echo [2/11] Generating DocSpace C# SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsCSharp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd ../../../../../sdk/docspace-api-sdk-csharp

echo [3/11] dotnet build install in csharp-sdk...
call dotnet build || goto :error
xcopy "src\DocSpace.API.SDK\bin\Debug\*.nupkg" "..\..\.nuget\packages" /s /y /b /i
cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK

echo [4/11] Generating DocSpace Python SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsPython.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [5/11] Generating DocSpace Postman SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsPostmanCollection.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [6/11] Generating DocSpace Typescript SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsTypeScript.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error
cd ../../../../../sdk/docspace-api-sdk-typescript

echo [7/11] npm install in typescript-sdk...
call npm install || goto :error
call npm pack || goto :error
xcopy "onlyoffice-docspace-api-sdk-*.tgz" "..\..\..\client\libs\ui-kit"  /s /y /b /i
cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK

echo [8/11] Generating DocSpace Java SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsJava.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [9/11] Generating DocSpace Kotlin SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsKotlin.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [10/11] Generating DocSpace PHP SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsPhp.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

echo [11/11] Generating DocSpace Swift6 SDK with OpenAPI Generator...
call openapi-generator-cli generate -c tools/DocSpace/toolsSwift6.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar || goto :error

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

