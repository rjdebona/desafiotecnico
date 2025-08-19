$ErrorActionPreference='Stop'
Write-Output "Starting E2E smoke test"
$res = Invoke-RestMethod -Method Post -Uri 'http://localhost:5007/auth/token' -Body (@{ username='admin'; password='password' } | ConvertTo-Json) -ContentType 'application/json'
$t = $res.token
Write-Output "Token: $t"
if ($null -eq $t) { Write-Error 'No token returned'; exit 1 }
$entryUrl = 'http://localhost:5260/api/FluxoDeCaixa'
$payload = @{ Id = [guid]::NewGuid().ToString(); Nome = 'Test Fluxo' } | ConvertTo-Json
Write-Output "Posting to $entryUrl"
try {
    $r = Invoke-RestMethod -Method Post -Uri $entryUrl -Body $payload -ContentType 'application/json' -Headers @{ Authorization = "Bearer $t" } -ErrorAction Stop
    Write-Output 'POST succeeded:'
    $r | ConvertTo-Json
} catch {
    Write-Output 'POST failed with error:'
    Write-Output $_.Exception.Message
    exit 2
}
