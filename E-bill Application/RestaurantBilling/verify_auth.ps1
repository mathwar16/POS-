$proc = Start-Process dotnet -ArgumentList "run" -WorkingDirectory "d:/RestaurantBilling" -PassThru -NoNewWindow
Write-Host "Starting server..."
Start-Sleep -Seconds 20

try {
    # 1. Signup
    Write-Host "Testing Signup..."
    $signupBody = @{
        name = "Test User"
        email = "test@example.com"
        password = "password123"
    } | ConvertTo-Json
    
    $signupUrl = "http://localhost:5108/api/auth/signup"
    $response = Invoke-RestMethod -Uri $signupUrl -Method Post -Body $signupBody -ContentType "application/json"
    Write-Host "Signup Success: $($response.token -ne $null)"
    $token = $response.token

    if ($token) {
        # 2. Login
        Write-Host "Testing Login..."
        $loginBody = @{
            email = "test@example.com"
            password = "password123"
        } | ConvertTo-Json
        
        $loginUrl = "http://localhost:5108/api/auth/login"
        $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
        Write-Host "Login Success: $($loginResponse.token -ne $null)"

        # 3. Access Secured Endpoint (Products)
        Write-Host "Testing Secured Endpoint (Products)..."
        $productsUrl = "http://localhost:5108/api/products"
        $headers = @{ Authorization = "Bearer $token" }
        try {
            $products = Invoke-RestMethod -Uri $productsUrl -Method Get -Headers $headers
            Write-Host "Products Access Success: Request completed"
        } catch {
             Write-Host "Products Access Failed: $($_.Exception.Message)"
        }

        # 4. Access Secured Endpoint without Token
        Write-Host "Testing Unsecured Access..."
        try {
            Invoke-RestMethod -Uri $productsUrl -Method Get
            Write-Host "Unsecured Access Failed: Did not throw 401"
        } catch {
            Write-Host "Unsecured Access Success: Threw $($_.Exception.Message)"
        }
    } else {
        Write-Host "Signup failed, no token received."
    }

} catch {
    Write-Host "Fatal Error: $($_.Exception.Message)"
    Write-Host "Response Body: $($_.ErrorDetails.Message)"
} finally {
    Write-Host "Stopping server..."
    if ($proc -and !$proc.HasExited) {
        Stop-Process -Id $proc.Id -Force
    }
}
