
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [Parameter()]
    [string]
    $PSHeader,

    [Parameter()]
    [string]
    $CSharpHeader,

    [Parameter()]
    [string]
    $RepoRoot = $PWD,

    [Parameter()]
    [string[]]
    $ExcludePaths = @('out')
)

if (-not $PSHeader)
{
    $PSHeader = @'

# Copyright (c) Microsoft Corporation.
# All rights reserved.


'@
}

$psHeaderTrimmed = $PSHeader.Trim()

if (-not $CSharpHeader)
{
    $CSharpHeader = @'

// Copyright (c) Microsoft Corporation.
// All rights reserved.


'@
}

$csharpHeaderTrimmed = $CSharpHeader.Trim()

$ExcludePaths = $ExcludePaths | ForEach-Object { "$RepoRoot/$_" }

:loop foreach ($file in Get-ChildItem -Path $RepoRoot -Recurse -Exclude $ExcludePaths)
{
    if ($file -is [System.IO.DirectoryInfo])
    {
        continue
    }

    $content = Get-Content -Raw $file.FullName
    switch ($file.Extension)
    {
        '.cs'
        {
            if ($content.TrimStart().StartsWith($csharpHeaderTrimmed))
            {
                continue loop
            }

            $content = $CSharpHeader + $content
            $encoding = 'utf8NoBOM'
            break
        }

        { $_ -in '.ps1', '.psm1' }
        {
            if ($content.TrimStart().StartsWith($psHeaderTrimmed))
            {
                continue loop
            }

            $content = $PSHeader + $content
            $encoding = 'utf8BOM'
            break
        }

        default
        {
            continue loop
        }
    }

    Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding $encoding
}
