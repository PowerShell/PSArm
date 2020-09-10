if (Get-Command Invoke-Build -ErrorAction Ignore)
{
    return
}

Install-Module InvokeBuild -Force -Scope CurrentUser