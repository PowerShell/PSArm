
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (Get-Command Invoke-Build -ErrorAction Ignore)
{
    return
}

Install-Module InvokeBuild -Force -Scope CurrentUser