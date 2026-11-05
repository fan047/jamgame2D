@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist "%~dp0HoledownGrowth.exe" (
  echo 未找到 HoledownGrowth.exe。请将本脚本与完整解压后的游戏程序放在同一目录。
  echo 按任意键关闭窗口。
  pause >nul
  exit /b 1
)
start "" "%~dp0HoledownGrowth.exe" --planner-test
