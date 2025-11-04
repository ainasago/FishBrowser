@echo off
cd /d "d:\1Dev\webbrowser\web\FishBrowser.Core"
dotnet build > build_output.txt 2>&1
type build_output.txt