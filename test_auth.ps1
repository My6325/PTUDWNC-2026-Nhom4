$baseUrl = "http://localhost:5000/api/v1/auth"
$email = "test.$(Get-Random)@example.com"
$password = "Password123!"

Write-Host "1) Registering..."
$regBody = @{ email = $email; password = $password; displayName = "Test User"; userName = "testuser_$(Get-Random)" } | ConvertTo-Json
$regRes = Invoke-WebRequest -Uri "$baseUrl/register" -Method Post -Body $regBody -ContentType "application/json" -ErrorAction SilentlyContinue
Write-Host "Status: $($regRes.StatusCode)"
Write-Host ""

Write-Host "2) Logging in..."
$loginBody = @{ email = $email; password = $password } | ConvertTo-Json
$loginRes = Invoke-WebRequest -Uri "$baseUrl/login" -Method Post -Body $loginBody -ContentType "application/json" -ErrorAction SilentlyContinue
$loginContent = $loginRes.Content | ConvertFrom-Json
$token = $loginContent.accessToken
$refreshToken = $loginContent.refreshToken
Write-Host "Status: $($loginRes.StatusCode)"
Write-Host "Body: (hidden)"
Write-Host ""

Write-Host "3) Get /me..."
$meRes = Invoke-WebRequest -Uri "$baseUrl/me" -Headers @{ Authorization = "Bearer $token" } -ErrorAction SilentlyContinue
Write-Host "Status: $($meRes.StatusCode)"
Write-Host "Body: $($meRes.Content)"
Write-Host ""

Write-Host "4) Patch /profile..."
$patchBody = @{ displayName = "Updated Name"; bio = "New bio" } | ConvertTo-Json
$patchRes = Invoke-WebRequest -Uri "$baseUrl/profile" -Method Patch -Body $patchBody -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" } -ErrorAction SilentlyContinue
Write-Host "Status: $($patchRes.StatusCode)"
Write-Host "Body: $($patchRes.Content)"
Write-Host ""

Write-Host "5) Refresh..."
$refreshBody = @{ refreshToken = $refreshToken } | ConvertTo-Json
$refreshRes = Invoke-WebRequest -Uri "$baseUrl/refresh" -Method Post -Body $refreshBody -ContentType "application/json" -ErrorAction SilentlyContinue
$refreshContent = $refreshRes.Content | ConvertFrom-Json
$newToken = $refreshContent.accessToken
$newRefreshToken = $refreshContent.refreshToken
Write-Host "Status: $($refreshRes.StatusCode)"
Write-Host "Body: (hidden)"
Write-Host ""

Write-Host "6) Reusing old refresh token (after 31s)..."
Start-Sleep -Seconds 31
try {
    $reuseRes = Invoke-WebRequest -Uri "$baseUrl/refresh" -Method Post -Body $refreshBody -ContentType "application/json"
    Write-Host "Status: $($reuseRes.StatusCode)"
} catch {
    Write-Host "Status: $($_.Exception.Response.StatusCode)"
    Write-Host "Body: $($_.Exception.Response.Content)"
}
Write-Host ""

Write-Host "7) Login again..."
$loginRes2 = Invoke-WebRequest -Uri "$baseUrl/login" -Method Post -Body $loginBody -ContentType "application/json" -ErrorAction SilentlyContinue
$loginContent2 = $loginRes2.Content | ConvertFrom-Json
$token2 = $loginContent2.accessToken
$refreshToken2 = $loginContent2.refreshToken
Write-Host "Status: $($loginRes2.StatusCode)"
Write-Host ""

Write-Host "8) Logout..."
$logoutBody = @{ refreshToken = $refreshToken2 } | ConvertTo-Json
$logoutRes = Invoke-WebRequest -Uri "$baseUrl/logout" -Method Post -Body $logoutBody -ContentType "application/json" -Headers @{ Authorization = "Bearer $token2" } -ErrorAction SilentlyContinue
Write-Host "Status: $($logoutRes.StatusCode)"
Write-Host ""

Write-Host "9) Refresh with logged out token..."
try {
    $refreshOut = Invoke-WebRequest -Uri "$baseUrl/refresh" -Method Post -Body $logoutBody -ContentType "application/json"
} catch {
    Write-Host "Status: $($_.Exception.Response.StatusCode)"
    Write-Host "Body: $($_.Exception.Response.Content)"
}
