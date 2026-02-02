@echo off

echo [1/4] Building OpenAPI documentation
cd ..\..\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln

echo [2/4] Generating OpenAPI specifications
cd ASC.Api.Documentation\json
call redocly join api_common.json people_common.json files_common.json backup_common.json ai_common.json ..\..\..\CustomGenerators\json\oauth.json -o ..\..\..\CustomGenerators\json\api-docs.json

echo [3/4] Post-processing: sorting tag groups
cd ..\bin\Debug\net10.0
call dotnet ASC.Api.Documentation.dll sort-tag-groups

echo [4/4] Post-processing: normalizing enums
call dotnet ASC.Api.Documentation.dll remove-enum

echo Documentation fix scripts completed successfully.
pause