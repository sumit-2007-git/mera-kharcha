@echo off
title Mera Kharcha - Instant Public Live Tunnel
color 0A
cls
echo ================================================================
echo           MERA KHARCHA - INSTANT PUBLIC LIVE TUNNEL
echo ================================================================
echo.
echo [*] Checking if MeraKharcha is running on http://localhost:5217 ...
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5217' -TimeoutSec 2 -UseBasicParsing; Write-Host '[OK] MeraKharcha is running!' -ForegroundColor Green } catch { Write-Host '[!] Note: Make sure MeraKharcha is running in Visual Studio (Play button)!' -ForegroundColor Yellow }"
echo.
echo [*] Generating secure Public HTTPS link (No login required)...
echo ================================================================
echo.
ssh -o StrictHostKeyChecking=no -R 80:localhost:5217 nokey@localhost.run
pause
