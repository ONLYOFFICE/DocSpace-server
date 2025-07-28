@echo off

echo [1/2] Running sortTagGroups.js to sort tag groups...
call node sortTagGroups.js

echo [2/2] Running removeStringEnum.js to clean up string enums...
call node removeStringEnum.js

echo Documentation fix scripts completed successfully.
pause