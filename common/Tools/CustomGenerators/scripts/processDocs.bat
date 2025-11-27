@echo off

echo [2/4] Building openapi Documentation
cd ..\..\ASC.Api.Documentation
call dotnet build ASC.Api.Documentation.sln
cd ASC.Api.Documentation\bin\Debug\net9.0
call redocly join asc.web.api.swagger.json asc.people.swagger.json asc.files.swagger.json asc.data.backup.swagger.json ..\..\..\..\..\CustomGenerators\json\oauth.json -o ..\..\..\..\..\CustomGenerators\json\api-docs.json
cd ..\..\..\..\..\CustomGenerators\scripts

echo [3/4] Running sortTagGroups.js to sort tag groups...
call node sortTagGroups.js

echo [4/4] Running removeStringEnum.js to clean up string enums...
call node removeStringEnum.js

echo Documentation fix scripts completed successfully.
pause