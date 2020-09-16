param(
    [Parameter()]
    [string]
    $ModulePath
)

Write-Host "Publishing module!"
Get-Item $ModulePath
