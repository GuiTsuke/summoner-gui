#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$GodotPath,
    [string]$OutputDirectory,
    [switch]$Capture,
    [switch]$PresentationOnly,
    [switch]$ElementalOnly,
    [switch]$RequireCompleteCoverage,
    [ValidateRange(10, 600)][int]$StepTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
if (($PresentationOnly -or $ElementalOnly) -and $RequireCompleteCoverage) {
    throw 'Os modos focados não executam simulação completa. Use -RequireCompleteCoverage separadamente.'
}
$projectDirectory = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $projectDirectory ('TestResults\validation-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw 'Use um diretório de saída novo para não misturar evidências de execuções diferentes.'
}
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
Set-Content -LiteralPath (Join-Path $OutputDirectory '.gdignore') -Value '' -Encoding UTF8

$steps = [Collections.Generic.List[object]]::new()
$report = [ordered]@{
    startedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    project = $projectDirectory
    environment = $null
    executionPassed = $false
    requestedCoverageComplete = $false
    steps = $steps
    simulations = @()
    missingRules = @()
    ruleChecks = @()
    visualInspection = 'Nenhuma inspeção humana é declarada pelo runner. -Capture produz imagens para inspeção.'
}

function Save-Report {
    $report | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'validation.json') -Encoding UTF8
}

function Stop-OwnedProcessTree([int]$ProcessId) {
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) { Stop-OwnedProcessTree -ProcessId $child.ProcessId }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Invoke-Check {
    param([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$Marker)
    $stdout = Join-Path $OutputDirectory ($Name + '.stdout.log')
    $stderr = Join-Path $OutputDirectory ($Name + '.stderr.log')
    # Start-Process joins ArgumentList; quote each argument as a Windows argv token.
    $quoted = foreach ($argument in $Arguments) {
        '"' + [regex]::Replace([regex]::Replace($argument, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1') + '"'
    }
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $Executable
    $startInfo.Arguments = $quoted -join ' '
    $startInfo.WorkingDirectory = $projectDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $null = $process.Start()
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($StepTimeoutSeconds * 1000)
    if ($timedOut) { Stop-OwnedProcessTree -ProcessId $process.Id }
    $process.WaitForExit()
    [IO.File]::WriteAllText($stdout, $outputTask.GetAwaiter().GetResult())
    [IO.File]::WriteAllText($stderr, $errorTask.GetAwaiter().GetResult())
    $stopwatch.Stop()
    $log = [IO.File]::ReadAllText($stdout) + "`n" + [IO.File]::ReadAllText($stderr)
    $errors = @([regex]::Matches($log, '(?im)^\s*(ERROR:|SCRIPT ERROR:|Unhandled exception)'))
    $warnings = @([regex]::Matches($log, '(?im)^\s*WARNING:'))
    $exitCode = if ($timedOut) { -1 } else { $process.ExitCode }
    $markerFound = -not $Marker -or $log.Contains($Marker)
    $passed = -not $timedOut -and $exitCode -eq 0 -and $errors.Count -eq 0 -and $markerFound
    $step = [ordered]@{
        name = $Name; executable = $Executable; arguments = $Arguments
        passed = $passed; exitCode = $exitCode; timedOut = $timedOut
        markerFound = $markerFound; errorCount = $errors.Count; warningCount = $warnings.Count
        elapsedSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
        stdout = $stdout; stderr = $stderr
    }
    $steps.Add($step)
    Save-Report
    Write-Host "$Name : passed=$passed, exit=$exitCode, errors=$($errors.Count), warnings=$($warnings.Count)"
    if (-not $passed) {
        throw "Falha em $Name. Consulte $stdout e $stderr. Timeout=$timedOut; marcador=$markerFound."
    }
    return $log
}

try {
    $dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1
    $candidates = [Collections.Generic.List[string]]::new()
    if ($GodotPath) {
        $candidates.Add((Resolve-Path -LiteralPath $GodotPath).Path)
    }
    else {
        Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^Godot.*\.exe$' -and $_.ExecutablePath } |
            Sort-Object { $_.CommandLine -notmatch '--editor' } | ForEach-Object { $candidates.Add($_.ExecutablePath) }
        Get-Command '*godot*' -CommandType Application -ErrorAction SilentlyContinue |
            ForEach-Object { $candidates.Add($_.Source) }
    }
    $engine = $null
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        $engineDirectory = Split-Path -Parent $candidate
        if (Test-Path -LiteralPath (Join-Path $engineDirectory 'GodotSharp')) {
            $console = Get-ChildItem -LiteralPath $engineDirectory -File |
                Where-Object { $_.Name -match 'console\.exe$' } | Select-Object -First 1
            $engine = if ($console) { $console.FullName } else { $candidate }
            break
        }
    }
    if (-not $engine) {
        throw 'Godot .NET não localizada nos processos/PATH. Abra o editor .NET ou informe -GodotPath <executável>. Nenhuma ferramenta será instalada.'
    }
    $engineVersion = (Invoke-Check -Name 'godot-version' -Executable $engine -Arguments @('--version')).Trim()
    $dotnetInfo = Invoke-Check -Name 'dotnet-info' -Executable $dotnetCommand.Source -Arguments @('--info')
    [xml]$project = Get-Content -Raw -LiteralPath (Join-Path $projectDirectory 'SummonerGui.csproj')
    $report.environment = @{
        godotPath = $engine; godotVersion = $engineVersion; dotnetPath = $dotnetCommand.Source
        projectSdk = $project.Project.Sdk; targetFramework = $project.Project.PropertyGroup.TargetFramework
        powershellVersion = $PSVersionTable.PSVersion.ToString()
    }
    $null = Invoke-Check -Name 'build' -Executable $dotnetCommand.Source -Arguments @('build', 'SummonerGui.csproj')
    $common = @('--headless', '--path', $projectDirectory)
    $null = Invoke-Check -Name 'import' -Executable $engine -Arguments ($common + @('--editor', '--import'))
    $null = Invoke-Check -Name 'main-smoke' -Executable $engine -Arguments ($common + @('--fixed-fps', '60', 'res://Tests/MainSmokeChecks.tscn')) -Marker 'MainSmokeChecks: PASS'
    if (-not $ElementalOnly) {
    $artArguments = @('--path', $projectDirectory, '--fixed-fps', '60')
    if ($Capture) { $artArguments += @('--disable-vsync', '--max-fps', '0', '--audio-driver', 'Dummy') }
    else { $artArguments += '--headless' }
    $artArguments += @('res://Tests/ArtChecks.tscn', '--', '--report-dir', $OutputDirectory)
    if ($Capture) { $artArguments += '--capture' }
    $null = Invoke-Check -Name 'art' -Executable $engine -Arguments $artArguments -Marker 'todos os recursos passaram.'
    }
    $null = Invoke-Check -Name 'combat' -Executable $engine -Arguments ($common + @('--fixed-fps', '60', 'res://Tests/CombatChecks.tscn')) `
        -Marker 'CombatChecks: todos os testes passaram.'
    $null = Invoke-Check -Name 'match' -Executable $engine -Arguments ($common + @('--fixed-fps', '60', 'res://Tests/MatchChecks.tscn')) `
        -Marker 'MatchChecks: todos os testes passaram.'
    if (-not $ElementalOnly) {
    $presentationArguments = @('--path', $projectDirectory, '--fixed-fps', '60')
    if ($Capture) { $presentationArguments += @('--disable-vsync', '--max-fps', '0', '--audio-driver', 'Dummy') }
    else { $presentationArguments += '--headless' }
    $presentationArguments += @('res://Tests/PresentationChecks.tscn', '--', '--report-dir', $OutputDirectory)
    if ($Capture) { $presentationArguments += '--capture' }
    $null = Invoke-Check -Name 'presentation' -Executable $engine -Arguments $presentationArguments `
        -Marker 'PresentationChecks: PASS'
    }
    $elementalArguments = @('--path', $projectDirectory, '--fixed-fps', '60')
    if ($Capture) { $elementalArguments += @('--disable-vsync', '--max-fps', '0', '--audio-driver', 'Dummy') }
    else { $elementalArguments += '--headless' }
    $elementalArguments += @('res://Tests/ElementalChecks.tscn', '--', '--report-dir', $OutputDirectory)
    if ($Capture) { $elementalArguments += '--capture' }
    $null = Invoke-Check -Name 'elemental' -Executable $engine -Arguments $elementalArguments -Marker 'ElementalChecks: PASS'
    if ($PresentationOnly -or $ElementalOnly) {
        $report.executionPassed = $true
        $report.scope = if ($ElementalOnly) { 'V5: Main, combate, compras/evolução, efeitos e queimadura; sem simulação completa.' } else { 'Apresentação, áudio e ataques elementais; sem simulação completa.' }
        Save-Report
        Write-Host "Validação focada passou. Áudio não ouvido; capturas requerem inspeção. Evidências: $OutputDirectory"
        exit 0
    }
    $simulationArguments = @('--path', $projectDirectory, '--fixed-fps', '60')
    if ($Capture) { $simulationArguments += @('--disable-vsync', '--max-fps', '0', '--audio-driver', 'Dummy') }
    else { $simulationArguments += '--headless' }
    $simulationArguments += @('res://Tests/SimulationChecks.tscn', '--', '--report-dir', $OutputDirectory)
    if ($Capture) { $simulationArguments += '--capture' }
    $null = Invoke-Check -Name 'simulation' -Executable $engine -Arguments $simulationArguments `
        -Marker 'SimulationChecks: todos os cenários executados e conciliados.'
    $report.simulations = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $OutputDirectory 'simulation.json') | ConvertFrom-Json
    $report.executionPassed = $true
    $report.requestedCoverageComplete = $true
    $report.ruleChecks = @(
        @{ status = 'PASS'; rule = 'Linhagem única, inclusive após evolução'; suite = 'MatchChecks' },
        @{ status = 'PASS'; rule = 'Compras e melhorias sem saldo e limite'; suite = 'MatchChecks' },
        @{ status = 'PASS'; rule = 'Evolução preserva unidade, nível, posição, alcance e intervalo'; suite = 'MatchChecks' },
        @{ status = 'PASS'; rule = 'Mover e trocar somente entre ondas, cancelar e limpar modo'; suite = 'MatchChecks' },
        @{ status = 'PASS'; rule = 'Recompensa e resultado únicos; animação não mantém ameaça'; suite = 'MatchChecks/CombatChecks' },
        @{ status = 'PASS'; rule = 'Dez ondas, um chefe como último surgimento, vitória aguarda restantes'; suite = 'MatchChecks/SimulationChecks' },
        @{ status = 'PASS'; rule = 'Vitória, derrota comum/chefe e reinício completo'; suite = 'MatchChecks/SimulationChecks' }
    )
    Save-Report
    $report.simulations | Select-Object name,outcome,waveReached,configuredWaves,coins,lives,summons,upgrades | Format-Table | Out-Host
    Write-Host 'Execução e regras automatizadas validadas. Inspeção visual/humana continua separada.'
    Write-Host "Evidências: $OutputDirectory"
    if ($RequireCompleteCoverage -and -not $report.requestedCoverageComplete) { exit 2 }
    exit 0
}
catch {
    $report.failure = $_.Exception.Message
    Save-Report
    Write-Error $_ -ErrorAction Continue
    Write-Host "Evidências parciais: $OutputDirectory"
    exit 1
}
