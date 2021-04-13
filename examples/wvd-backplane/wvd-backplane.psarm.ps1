param(
  [string]
  $hostpoolName = 'myFirstHostpool',

  [string]
  $appgroupName = 'myFirstAppGroup',

  [string]
  $workspaceName = 'myFirstWorkspace'
)

Arm {
  param(

    [ArmParameter[string]]
    $hostpoolFriendlyName = 'My Bicep created Host pool',

    [ArmParameter[string]]
    $appgroupNameFriendlyName = 'My Bicep created AppGroup',

    [ArmParameter[string]]
    $workspaceNameFriendlyName = 'My Bicep created Workspace',

    [ValidateSet('Desktop', 'RemoteApp')]
    [ArmParameter[string]]
    $applicationgrouptype = 'Desktop',

    [ValidateSet('Desktop', 'RailApplications')]
    [ArmParameter[string]]
    $preferredAppGroupType = 'Desktop',

    [ArmParameter[string]]
    $wvdbackplanelocation = 'eastus',

    [ArmParameter[string]]
    $hostPoolType = 'pooled',

    [ArmParameter[string]]
    $loadBalancerType = 'BreadthFirst'
  )

  Resource $hostpoolName -Namespace 'Microsoft.DesktopVirtualization' -Type 'hostPools' -ApiVersion '2019-12-10-preview' -Location $wvdbackplanelocation {
    properties {
      friendlyName $hostpoolFriendlyName
      hostPoolType $hostPoolType
      loadBalancerType $loadBalancerType
      preferredAppGroupType $preferredAppGroupType
    }
  }

  Resource $appgroupName -Namespace 'Microsoft.DesktopVirtualization' -Type 'applicationGroups' -ApiVersion '2019-12-10-preview' -Location $wvdbackplanelocation {
    properties {
      friendlyName $appgroupNameFriendlyName
      applicationGroupType $applicationgrouptype
      hostPoolArmPath (resourceId 'Microsoft.DesktopVirtualization/hostPools' $hostpoolName)
    }
    DependsOn @(
      resourceId 'Microsoft.DesktopVirtualization/hostPools' $hostpoolName
    )
  }

  Resource $workspaceName -Namespace 'Microsoft.DesktopVirtualization' -Type 'workspaces' -ApiVersion '2019-12-10-preview' -Location $wvdbackplanelocation {
    properties {
      friendlyName $workspaceNameFriendlyName
      applicationGroupReferences (resourceId 'Microsoft.DesktopVirtualization/applicationGroups' $appgroupName)
    }
    DependsOn @(
      resourceId 'Microsoft.DesktopVirtualization/applicationGroups' $appgroupName
    )
  }
}
