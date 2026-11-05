@echo off
cd /d "%~dp0"
if not exist "%~dp0HoledownGrowth.exe" (
  echo HoledownGrowth.exe was not found. Run this script beside the extracted Player executable.
  pause
  exit /b 1
)
start "" "%~dp0HoledownGrowth.exe" --planner-test
