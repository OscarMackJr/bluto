param(
    [ValidateSet("info", "validate", "migrate", "repair")]
    [string] $Command = "validate",

    [string] $FlywayExecutable = "C:\tools\flyway-13.1.0\flyway.cmd",

    [string] $Url = $env:BLUTO_FLYWAY_URL,

    [string] $User = $env:BLUTO_FLYWAY_USER,

    [string] $Password = $env:BLUTO_FLYWAY_PASSWORD
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $FlywayExecutable)) {
    throw "Flyway executable not found at '$FlywayExecutable'. Pass -FlywayExecutable or install Flyway there."
}

if ([string]::IsNullOrWhiteSpace($Url)) {
    throw "Missing Flyway JDBC URL. Set BLUTO_FLYWAY_URL or pass -Url, for example jdbc:postgresql://localhost:15432/bluto."
}

if ([string]::IsNullOrWhiteSpace($User)) {
    throw "Missing Flyway user. Set BLUTO_FLYWAY_USER or pass -User."
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    throw "Missing Flyway password. Set BLUTO_FLYWAY_PASSWORD or pass -Password."
}

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")
$config = Join-Path $repoRoot "implementation\tools\flyway\flyway.local.conf"
$args = @(
    "-configFiles=$config",
    "-workingDirectory=$repoRoot",
    "-url=$Url",
    "-user=$User",
    "-password=$Password",
    $Command
)

& $FlywayExecutable @args
exit $LASTEXITCODE
