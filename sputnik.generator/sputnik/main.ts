/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { AutoRestExtension, } from '@azure-tools/autorest-extension-base';
import { generator } from './generator';

require('source-map-support').install();

export async function main() {
  const pluginHost = new AutoRestExtension();
  pluginHost.Add('sputnik', generator);
  await pluginHost.Run();
}

main();
