@echo off

echo [2/4] Building openapi Documentation
cd ..\..\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln
cd ASC.Api.Documentation\bin\Debug\net8.0
call redocly join asc.web.api.swagger.json asc.people.swagger.json asc.files.swagger.json asc.data.backup.swagger.json ..\..\..\..\..\CustomGenerators\tools\oauth.json -o ..\..\..\..\..\CustomGenerators\tools\api-docs.json
cd ..\..\..\..\..\CustomGenerators\tools

echo [3/4] Running sortTagGroups.js to sort tag groups...
call node sortTagGroups.js

echo [4/4] Running removeStringEnum.js to clean up string enums...
call node removeStringEnum.js

echo Documentation fix scripts completed successfully.
pause