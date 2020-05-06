$Script:schema = [Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)

$subnets = @{}
$subnets['addressPrefix'] = @{ Name = 'addressPrefix'; Type = 'string'; Required = $false; }
$subnets['addressPrefixes'] = @{ Name = 'addressPrefixes'; Type = 'array'; Required = $false; }

$Script:schema[''] = $subnets

function Get-Schema
{
    [Parameter(Mandatory, Position = 0)]
    param([string] $name)
    return $Script:schema[$name]
}
