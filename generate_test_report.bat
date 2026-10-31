@echo off
setlocal

if exist ".\TestResults" rd /s /q ".\TestResults"

echo Running tests...
dotnet test src\UniiaAnonim.TGBot.Tests\UniiaAnonim.TGBot.Tests.csproj ^
    --results-directory "./TestResults" ^
    --coverage ^
    --coverage-output-format cobertura ^
    --coverage-output coverage.cobertura.xml

echo Generating report...
reportgenerator ^
    -reports:"./TestResults/**/coverage.cobertura.xml;./TestResults/coverage.cobertura.xml" ^
    -targetdir:./TestResults/Report ^
    -reporttypes:Html ^
    -filefilters:"-*.generated.cs;-*.g.cs" ^
    -classfilters:"-*Microsoft.AspNetCore.OpenApi*" ^
    -sourcedirs:./src ^
    -verbosity:Error

echo.
echo ========================================================
echo Report generated successfully in: ./TestResults/Report/index.html
echo ========================================================
echo.

pause