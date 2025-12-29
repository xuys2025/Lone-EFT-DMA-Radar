@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem One-click cleanup for build outputs & caches.
rem Default: clean repo build artifacts only.
rem Options:
rem   CLEAN_GENERATED.bat /all   -> also clear NuGet global caches (outside repo)
rem   CLEAN_GENERATED.bat /nuget -> same as /all

cd /d "%~dp0"

set "DO_NUGET=0"
if /I "%~1"=="/all" set "DO_NUGET=1"
if /I "%~1"=="/nuget" set "DO_NUGET=1"

echo ============================================================
echo Clean target: %CD%
echo.
echo Repo items that will be deleted:
echo   - src\**\bin\
echo   - src\**\obj\
if "%DO_NUGET%"=="1" (
  echo.
  echo NuGet caches that will be cleared (outside repo):
  echo   - dotnet nuget locals all --clear
)
echo ============================================================
echo.
set /p "CONFIRM=Continue? (Y/N): "
if /I not "%CONFIRM%"=="Y" (
  echo Aborted.
  exit /b 1
)

echo.
echo [1/2] Deleting repo build folders...

rem Delete any bin/obj under src (covers future multi-project layouts)
for /d /r "src" %%D in (bin obj) do (
  if exist "%%~fD" (
    echo   - rmdir /s /q "%%~fD"
    rmdir /s /q "%%~fD" >nul 2>&1
  )
)

if "%DO_NUGET%"=="1" (
  echo.
  echo [2/2] Clearing NuGet caches...
  dotnet nuget locals all --clear
) else (
  echo.
  echo [2/2] Skipping NuGet cache cleanup.
  echo   Tip: run ^"CLEAN_GENERATED.bat /all^" to clear NuGet caches too.
)

echo.
echo Done.
exit /b 0
