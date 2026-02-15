@echo off
echo Starting Restaurant E-bill Application...

:: Start Backend
echo Launching Backend (RestaurantBilling)...
start "Backend - RestaurantBilling" cmd /c "cd RestaurantBilling && dotnet run"

:: Start Frontend
echo Launching Frontend (chatgpt-ebill)...
:: We use live-server for a better experience, but if they don't have it, npx will handle it.
:: The --port=8080 and --no-browser flags ensure it runs consistently and we control the opening.
start "Frontend - chatgpt-ebill" cmd /c "cd chatgpt-ebill && npx live-server --port=8080 --no-browser"

:: Wait for servers to be ready
echo Waiting for servers to initialize...
timeout /t 8 /nobreak > nul

:: Open the browser
echo Opening the application...
start http://localhost:8080/index.html

echo.
echo Application is running!
echo Keep the terminal windows open while using the app.
pause
