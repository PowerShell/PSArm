
# Copyright (c) Microsoft Corporation.
# All rights reserved.

using namespace System.Collections.Generic

## what data structure should be used to hold schema?
## As a proof-of-concept sample, here I assume the schema only has info about available properties.
$Script:schema = [Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)

$networkInterface = @{}
$networkInterface['networkSecurityGroup'] = @{ Name = 'networkSecurityGroup'; Type = 'object'; Required = $false; Command = 'NetworkSecurityGroup' }
$networkInterface['ipConfigurations'] = @{ Name = 'ipConfigurations'; Type = 'array'; Required = $false; Command = 'NetworkInterfaceIPConfiguration' }
$networkInterface['dnsSettings'] = @{ Name = 'dnsSettings'; Type = 'object'; Required = $false; Command = 'NetworkInterfaceDnsSettings' }
$networkInterface['enableAcceleratedNetworking'] = @{ Name = 'enableAcceleratedNetworking'; Type = 'boolean'; Required = $false; }
$networkInterface['enableIPForwarding'] = @{ Name = 'enableIPForwarding'; Type = 'boolean'; Required = $false; }

$networkInterfaceIPConfiguration = @{}
$networkInterfaceIPConfiguration['subnet'] = @{ Name = 'subnet'; Type = 'object'; Required = $false; Command = 'Subnet' }
$networkInterfaceIPConfiguration['privateIPAddress'] = @{ Name = 'privateIPAddress'; Type = 'string'; Required = $false; }
$networkInterfaceIPConfiguration['privateIPAllocationMethod'] = @{ Name = 'privateIPAllocationMethod'; Type = 'enum'; Required = $false; Values = 'Static','Dynamic' }

$Script:schema[''] = $networkInterface
$Script:schema['networkInterfaceIPConfiguration'] = $networkInterfaceIPConfiguration

function Get-Schema
{
    [Parameter(Mandatory, Position = 0)]
    param([string] $name)
    return $Script:schema[$name]
}

function NetworkInterfaceIPConfiguration
{
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory, Position = 0)]
        [scriptblock] $Body
    )

    $expando = [ordered]@{ name = $Name }

    $properties = [ordered]@{}
    $results = & $Body
    switch ($results) {
        { $_.Type -eq 'Property' } {
            $properties[$_.Name] = $_.Value
        }
        default { throw "$($_.Type) not supported yet." }
    }

    if ($properties.Count -gt 0) {
        $expando.properties = $properties
    }

    return $expando
}

function Subnet
{
    param(
        [Parameter(Mandatory)]
        [string] $Id
    )

    return [ordered]@{ id = $Id }
}
