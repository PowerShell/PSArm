Describe "Basic PSArm template elements" {
    It "Generates a resource correctly" {
        $template = Arm {
            Resource "Example" -Location WestUS2 -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type virtualNetworks/subnets {
                Properties {
                    AddressPrefix 10.0.0.0/24
                }
            }
        }
        
        Assert-EquivalentToTemplate -GeneratedObject $template -TemplateDefinition @'
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "resources": [
    {
      "apiVersion": "2019-11-01",
      "type": "Microsoft.Network/virtualNetworks/subnets",
      "name": "Example",
      "location": "WestUS2",
      "properties": {
        "addressPrefix": "10.0.0.0/24"
      }
    }
  ]
}
'@
    }
}