@echo off

echo Building OpenAPI documentation
cd ..\..\..\..\..\..\

call dotnet build ASC.Web.sln -p:OpenApiGenerateDocuments=true

echo [1/2] Building ASC.Api.Documentation
cd common\Tools\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln

echo [2/2] Join OpenAPI documentation
cd ASC.Api.Documentation\bin\Debug\net10.0
call dotnet ASC.Api.Documentation.dll

echo Documentation fix scripts completed successfully.
pause