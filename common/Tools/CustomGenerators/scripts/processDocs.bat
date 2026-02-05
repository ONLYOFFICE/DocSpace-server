@echo off

echo Building OpenAPI documentation
cd ..\..\..\..\

call dotnet build ASC.Web.sln -p:OpenApiGenerateDocuments=true

echo [1/4] Building ASC.Api.Documentation
cd common\Tools\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln

echo [2/4] Generating OpenAPI specifications
cd ASC.Api.Documentation\json
call redocly join api_2.0.json people_2.0.json files_2.0.json backup_2.0.json ai_2.0.json ..\..\..\CustomGenerators\json\oauth.json -o ..\..\..\CustomGenerators\json\api-docs.json

echo [3/4] Post-processing: sorting tag groups
cd ..\bin\Debug\net10.0
call dotnet ASC.Api.Documentation.dll sort-tag-groups

echo [4/4] Post-processing: normalizing enums
call dotnet ASC.Api.Documentation.dll remove-enum

echo Documentation fix scripts completed successfully.
pause