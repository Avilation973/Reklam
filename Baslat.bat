@echo off
cd /d "%~dp0"
python optimizer.py
if errorlevel 1 pause

