param(
    [ValidateSet('consolidacao','lancamento')]
    [string]$Target = 'consolidacao',
    [int]$Vus = 50,
    [string]$Duration = '30s',
    [string]$ApiKey = 'dev-default-key',
    [string]$BaseUrl = '',
    [string]$SummaryFileName = 'k6-summary.json'
)

    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $absPath = (Resolve-Path $scriptDir).Path

    # Summary export will be written into the mounted scripts folder. Influx publishing support
    # has been removed from this repository. If you need to publish summaries, recreate a
    # push script and call it manually after running this script.
    $mount = "$($absPath):/scripts"

    # Build docker arguments robustly to avoid quoting/parsing issues
    $envArgs = @()
    if ($Target -eq 'consolidacao') {
        $envArgs += '-e'; $envArgs += "CONSOLIDACAO_BASE=$BaseUrl"
    } else {
        $envArgs += '-e'; $envArgs += "LANCAMENTO_BASE=$BaseUrl"
        $envArgs += '-e'; $envArgs += "API_KEY=$ApiKey"
    }
    $envArgs += '-e'; $envArgs += "AUTH_BASE=http://host.docker.internal:5080"
    $envArgs += '-e'; $envArgs += "ADMIN_USER=admin"
    $envArgs += '-e'; $envArgs += "ADMIN_PASS=password"

    $dockerArgs = @('run','--rm') + $envArgs + @('-v', $mount, '-w', '/scripts', 'grafana/k6', 'run', '--vus', $Vus.ToString(), '--duration', $Duration, '--summary-export', "/scripts/$SummaryFileName", $scriptFile)

    Write-Host "Executing: docker $($dockerArgs -join ' ')"

# default base URLs when none provided (use host.docker.internal for Docker)
    if ([string]::IsNullOrEmpty($BaseUrl)) {
        if ($Target -eq 'consolidacao') { $BaseUrl = 'http://host.docker.internal:5260' }
        else { $BaseUrl = 'http://host.docker.internal:5007' }
    }

    # pick the k6 script based on target
    if ($Target -eq 'consolidacao') { $scriptFile = 'script.js' } else { $scriptFile = 'create_lancamentos.js' }

    # Summary export will be written into the mounted scripts folder. Influx publishing support
    # has been removed from this repository. If you need to publish summaries, recreate a
    # push script and call it manually after running this script.
    $mount = "$($absPath):/scripts"

    # Build docker arguments robustly to avoid quoting/parsing issues
    $envArgs = @()
    if ($Target -eq 'consolidacao') {
        $envArgs += '-e'; $envArgs += "CONSOLIDACAO_BASE=$BaseUrl"
    } else {
        $envArgs += '-e'; $envArgs += "LANCAMENTO_BASE=$BaseUrl"
        $envArgs += '-e'; $envArgs += "API_KEY=$ApiKey"
    }
    $envArgs += '-e'; $envArgs += "AUTH_BASE=http://host.docker.internal:5080"
    $envArgs += '-e'; $envArgs += "ADMIN_USER=admin"
    $envArgs += '-e'; $envArgs += "ADMIN_PASS=password"

    $dockerArgs = @('run','--rm') + $envArgs + @('-v', $mount, '-w', '/scripts', 'grafana/k6', 'run', '--vus', $Vus.ToString(), '--duration', $Duration, '--summary-export', "/scripts/$SummaryFileName", $scriptFile)

    Write-Host "Executing: docker $($dockerArgs -join ' ')"

    & docker @dockerArgs
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { Write-Error "k6 run failed with exit code $exit"; exit $exit }
    Write-Host "k6 run completed successfully"

    # NOTE: Influx publishing support has been removed from the repository. To publish results,
    # manually use a push script or add your own implementation.
