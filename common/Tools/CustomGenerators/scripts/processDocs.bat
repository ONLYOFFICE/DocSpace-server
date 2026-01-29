@echo off

echo [1/4] Building OpenAPI documentation
cd ..\..\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln
cd ASC.Api.Documentation\bin\Debug\net10.0

echo [2/4] Generating OpenAPI specifications
call dotnet ASC.Api.Documentation.dll swagger --silent y
call redocly join asc.web.api.swagger.json asc.people.swagger.json asc.files.swagger.json asc.data.backup.swagger.json ..\..\..\..\..\CustomGenerators\json\oauth.json -o ..\..\..\..\..\CustomGenerators\json\api-docs.json

echo [3/4] Post-processing: sorting tag groups
call dotnet ASC.Api.Documentation.dll sort-tag-groups

echo [4/4] Post-processing: normalizing enums
call dotnet ASC.Api.Documentation.dll remove-enum

echo Documentation fix scripts completed successfully.
pause