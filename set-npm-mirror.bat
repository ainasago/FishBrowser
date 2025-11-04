@echo off
npm config set registry https://registry.npmmirror.com && npm config get registry && echo. && echo Mirror configured successfully! && pause
