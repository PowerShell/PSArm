
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Arm {
    Resource "mySubnet" -Namespace Microsoft.Network -ApiVersion 2019-11-01 -Type virtualNetworks/subnets {
        properties {
            addressPrefix 10.0.0.0/24
        }
    }
}