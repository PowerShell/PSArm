
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
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
    $RepoRoot = ((Resolve-Path "$PSScriptRoot/../").Path)
)

if (-not $PSHeader)
{
    $PSHeader = @'

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.


'@
}

$psHeaderTrimmed = $PSHeader.Trim()

if (-not $CSharpHeader)
{
    $CSharpHeader = @'

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


'@
}

$csharpHeaderTrimmed = $CSharpHeader.Trim()

:loop foreach ($file in Get-ChildItem -Path $RepoRoot -Recurse)
{
    Write-Verbose "Looking at file '$file'"
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

    Write-Host "Adding copyright header to '$file'"
    Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding $encoding
}
