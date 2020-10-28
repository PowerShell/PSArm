[CmdletBinding(DefaultParameterSetName="Execute")]
param(
    [Parameter(ParameterSetName="Execute")]
    [string]
    $PluginPath = $PSScriptRoot,

    [Parameter(ParameterSetName="Execute")]
    [string]
    $OutputPath = "$PSScriptRoot/out",

    [Parameter(ParameterSetName="Execute")]
    [string]
    $SpecRootPath = (Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'azure-api-rest-specs'),

    [Parameter(ParameterSetName="Execute")]
    [switch]
    $WaitSputnikDebugger,

    [Parameter(ParameterSetName="DotSource")]
    [switch]
    $NoExecute
)

function Get-ArmReadmes
{
    param(
        [Parameter(Mandatory)]
        [string]
        $SpecRootPath
    )

    Get-ChildItem -LiteralPath "$SpecRootPath/specification" |
        ForEach-Object { "$($_.FullName)/resource-manager/readme.azureresourceschema.md" } |
        Where-Object { Test-Path -LiteralPath $_ }
}

filter Get-TagsFromReadme
{
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        $ReadmePath
    )

    $basePath = Split-Path -Path $ReadmePath -Parent

    $resourceName = Split-Path -Path (Split-Path -Path $basePath -Parent) -Leaf

    $markdown = Get-Content -Raw -LiteralPath $ReadmePath | ConvertFrom-Markdown
    $codeblocks = $markdown.Tokens | Where-Object { $_ -is [Markdig.Syntax.CodeBlock] }

    $tags = ($codeblocks[0].Lines.ToString() | ConvertFrom-Yaml).batch.tag

    [pscustomobject]@{
        ResourceName = $resourceName
        Tags = $tags
        InputPath = Join-Path $basePath "readme.azureresourceschema.md"
    }
}

function Invoke-Autorest
{
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]
        $InputPath,

        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]
        $ResourceName,

        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string[]]
        $Tags,

        [Parameter()]
        [string]
        $PluginPath = '.',

        [Parameter()]
        [string]
        $OutputPath
    )

    begin
    {
        $failed = [System.Collections.Generic.List[object]]::new()
        $OutputPath = (Resolve-Path $OutputPath).Path.TrimEnd('\')
    }

    process
    {
        $errors = [System.Collections.Generic.List[System.Management.Automation.ErrorRecord]]::new()

        foreach ($tag in $Tags)
        {
            $resourceLabel = "$ResourceName|$tag"
            Write-Host "Generating output for '$resourceLabel'"

            $params = @(
                $InputPath
                "--use:$PluginPath"
                "--tag=$tag"
                '--azureresourceschema'
                if ($VerbosePreference) { '--debug', '--verbose' }
                if ($OutputPath) { "--output-directory:$OutputPath" }
            )
            Write-Verbose "Running: 'autorest $params'"
            autorest @params 2>&1 | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord])
                {
                    $errors.Add($_)
                }

                $_
            }

            if ($LASTEXITCODE -ne 0)
            {
                $failed.Add(@{
                    Tag = $tag
                    Errors = $errors.ToArray()
                })
            }

            $errors.Clear()
        }
    }

    end
    {
        foreach ($failure in $failed)
        {
            $tag = $failure.Tag
            Write-Warning "FAILED: '$tag'"
            $failure.Errors | ForEach-Object { "    $_" } | Write-Warning
            Write-Host
        }
    }
}

if ($NoExecute)
{
    return
}

Import-Module powershell-yaml -ErrorAction Stop

if (-not (Test-Path $SpecRootPath))
{
    if (-not (Get-Command -Name git -ErrorAction Ignore))
    {
        throw "'git' not found. Please install 'git' to continue"
    }

    git clone 'https://github.com/Azure/azure-rest-api-specs.git' $SpecRootPath

    if ($LASTEXITCODE -ne 0)
    {
        throw "Clone of spec repo failed. See above for error details"
    }
}

Get-ArmReadmes -SpecRootPath $SpecRootPath |
    Get-TagsFromReadme |
    Invoke-Autorest -OutputPath $OutputPath
