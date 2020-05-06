$Script:schema = [Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)

$publicIPAddress = @{}
$publicIPAddress['publicIPAllocationMethod'] = @{ Name = 'publicIPAllocationMethod'; Type = 'enum'; Required = $false; Values = 'Static','Dynamic' }
$publicIPAddress['publicIPAddressVersion'] = @{ Name = 'publicIPAddressVersion'; Type = 'enum'; Required = $false; Values = 'IPv4','IPv6' }

$Script:schema[''] = $publicIPAddress

function Get-Schema
{
    [Parameter(Mandatory, Position = 0)]
    param([string] $name)
    return $Script:schema[$name]
}
