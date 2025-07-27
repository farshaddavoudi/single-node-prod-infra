# Save this as setup-pwless-ssh-win-to-linux.ps1

param (
    [Parameter(Position=0, Mandatory=$true)]
    [string]$Target  # Format: user@remote-ip
)

# Path to public key
$pubKeyPath = "$env:USERPROFILE\.ssh\id_rsa.pub"

# Check if public key exists
if (-Not (Test-Path $pubKeyPath)) {
    Write-Host "❌ SSH public key not found at: $pubKeyPath"
    Write-Host "➡️ Generate one using: ssh-keygen"
    exit 1
}

Write-Host "🚀 Sending public key to $Target for passwordless SSH..."

Get-Content $pubKeyPath | ssh $Target "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && chmod 700 ~/.ssh"

Write-Host "✅ SSH key deployed. You should now be able to connect without a password:"
Write-Host "`n    ssh $Target"
