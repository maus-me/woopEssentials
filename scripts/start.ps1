# you may need to set in a powershell terminal the execution policy to allow execution of local scripts, can be done with below command
# Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

while (1) {
    Start-Process -FilePath "C:\path\to\Vintagestory\VintagestoryServer.exe" -Wait
    Write-Host "restarting..."
    Start-Sleep -s 2
}