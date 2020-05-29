/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import {
  Host,
  startSession,
  Session,
} from "@azure-tools/autorest-extension-base";
import {
  codeModelSchema,
  CodeModel,
  Language,
  ObjectSchema,
  ApiVersion,
  SchemaType,
  ArraySchema,
  Property,
  Schema,
  StringSchema,
  NumberSchema,
  ChoiceSchema,
  BooleanSchema,
} from "@azure-tools/codemodel";
import { values } from "@azure-tools/linq";
import * as path from "path";
import { promises as fsPromises } from "fs";
import { ParseOptions } from "querystring";
import { dir } from "console";

interface IResourceProvider {
  $keywords: Record<string, IKeyword>,
  $resources: Record<string, Record<string, KeywordPointer>>,
}

interface IKeyword {
  array?: boolean,
  parameters?: Record<string, IParameter>,
  propertyParameters?: Record<string, IParameter>,
  body?: Record<string, KeywordPointer>,
}

interface IParameter {
  type?: "string" | "int" | "bool",
  enum?: any[]
}

interface IStringParameter extends IParameter {
  type: "string",
  enum?: string[],
  pattern?: string,
}

interface IIntParameter extends IParameter {
  type: "int",
  enum?: number[],
}

class KeywordPointer {
  public ref: IKeyword;
  private path: string | undefined;

  constructor(
    public readonly builder: KeywordBuilder) {
    this.ref = builder.getKeyword();
    this.path = undefined;
  }

  public setPath(prefix: string) {
    const escapedRefName = this.builder.keywordRefKey?.replace(/~/g, '~0').replace(/\//g, '~1');
    this.path = `${prefix}/${escapedRefName}`;
  }

  public toJSON(): object {
    return {
      $ref: this.path,
    };
  }
}

class KeywordBuilder {
  private keyword: IKeyword;
  private pointer: KeywordPointer;
  private refKey: string | undefined;

  constructor(public readonly keywordName: string) {
    this.keyword = {};
    this.pointer = new KeywordPointer(this);
    this.refKey = undefined;
  }

  public addParameter(parameterName: string, parameter: IParameter) {
    if (!this.keyword.parameters) {
      this.keyword.parameters = {};
    }

    this.keyword.parameters[parameterName] = parameter;
  }

  public addPropertyParameter(parameterName: string, parameter: IParameter) {
    if (!this.keyword.propertyParameters) {
      this.keyword.propertyParameters = {};
    }

    this.keyword.propertyParameters[parameterName] = parameter;
  }

  public addBody(body: Record<string, KeywordPointer>) {
    this.keyword.body = body;
  }

  public makeArray() {
    this.keyword.array = true;
  }

  public getKeyword(): IKeyword {
    return this.keyword;
  }

  public getKeywordPointer(): KeywordPointer {
    return this.pointer;
  }

  public addToKeywordTable(refKey: string, table: Record<string, IKeyword>) {
    this.refKey = refKey;
    table[refKey] = this.keyword;
  }

  public get keywordRefKey() {
    return this.refKey;
  }
}

class KeywordTable {
  private duplicateMap: Map<Schema, KeywordBuilder>;
  private keywords: Record<string, IKeyword>;

  constructor() {
    this.duplicateMap = new Map<Schema, KeywordBuilder>();
    this.keywords = {};
  }

  public get(schema: Schema): KeywordPointer | undefined {
    const result = this.duplicateMap.get(schema);
    return result && result.getKeywordPointer();
  }

  public add(schema: Schema, keywordBuilder: KeywordBuilder) {
    this.duplicateMap.set(schema, keywordBuilder);

    let keywordRefKey: string = keywordBuilder.keywordName;
    let i: number = 2;
    while (this.keywords.hasOwnProperty(keywordRefKey)) {
      keywordRefKey = `${keywordBuilder.keywordName}_${i}`;
      i++;
    }

    keywordBuilder.addToKeywordTable(keywordRefKey, this.keywords);
  }

  public getKeywords(): Record<string, IKeyword> {
    return this.keywords;
  }

  public getPointers(): KeywordPointer[] {
    const result = [];
    for (const builder of this.duplicateMap.values()) {
      result.push(builder.getKeywordPointer());
    }
    return result;
  }
}

class ResourceProviderBuilder {
  private resources: Record<string, Record<string, KeywordPointer>>;
  private keywordTable: KeywordTable;

  constructor(
    private providerName: string,
    private apiVersion: string) {

    this.resources = {};
    this.keywordTable = new KeywordTable();
  }

  public writeToDir(dirPath: string): Promise<void> {
    const providerObject: IResourceProvider = {
      $keywords: this.keywordTable.getKeywords(),
      $resources: this.resources,
    };

    this.setReferencePaths(providerObject);

    // Set the file name and write out to file
    const filePath = path.join(dirPath, `${this.providerName}_${this.apiVersion}.json`);
    return fsPromises.writeFile(filePath, JSON.stringify(providerObject, null, '    '));
  }

  public addResource(resourceType: string, schema: ObjectSchema) {
    // Ensure the resource itself is there to add keywords to
    if (!this.resources.hasOwnProperty(resourceType)) {
      this.resources[resourceType] = {};
    }

    // Add top level keywords to the resource's keyword table
    // Accumulating those keywords and their children as we go for the keyword definition table
    this.addTopLevelKeywordsToTable(this.resources[resourceType], schema);
  }

  private setReferencePaths(providerObject: IResourceProvider) {
    const pathPrefix = "#/$keywords";

    for (const kwPtr of this.keywordTable.getPointers()) {
      kwPtr.setPath(pathPrefix);
    }
  }

  private addTopLevelKeywordsToTable(table: Record<string, KeywordPointer>, schema: ObjectSchema) {
    const propertiesField: ObjectSchema | undefined = <ObjectSchema>schema.properties?.find(p => p.serializedName === "properties")?.schema;

    if (!propertiesField?.properties) {
      return;
    }

    this.addKeywordsToTable(table, propertiesField.properties);
  }

  private addKeywordsToTable(table: Record<string, KeywordPointer>, properties: Property[]) {
    // Each keyword is a property of the "properties" property
    for (const property of properties) {
      const keywordName = property.serializedName;
      const keywordRef: KeywordPointer = this.getKeywordFromProperty(keywordName, property);
      table[keywordName] = keywordRef;
    }
  }

  private getKeywordFromProperty(keywordName: string, property: Property): KeywordPointer {
    const schema = property.schema;

    // Return early from the cache to prevent unbounded recursion
    const existingKeyword = this.keywordTable.get(schema);
    if (existingKeyword) {
      return existingKeyword;
    }

    // Create holders for the keyword
    const keywordBuilder = new KeywordBuilder(keywordName);

    // Set the keyword value before recursing
    this.keywordTable.add(schema, keywordBuilder);

    // Actually populate keyword
    this.buildKeywordFromSchema(keywordBuilder, property.schema);

    // Return a pointer to the keyword
    return keywordBuilder.getKeywordPointer();
  }

  private buildKeywordFromSchema(keyword: KeywordBuilder, schema: Schema) {
    switch (schema.type) {
      case SchemaType.Array:
        this.buildKeywordFromSchema(keyword, (<ArraySchema>schema).elementType);
        keyword.makeArray();
        return;

      case SchemaType.Object:
        this.buildKeywordFromObjectSchema(keyword, <ObjectSchema>schema);
        return;

      default:
        this.buildKeywordFromOtherSchema(keyword, schema);
        return;
    }
  }

  private buildKeywordFromOtherSchema(keyword: KeywordBuilder, schema: Schema) {
    // Implement a keyword with a single -Value parameter
    keyword.addPropertyParameter('value', this.getParameterFromSchema(schema));
  }

  private buildKeywordFromObjectSchema(keyword: KeywordBuilder, schema: ObjectSchema) {
    if (!schema.properties) {
      return;
    }

    let propertiesProperty: Property | undefined;
    let canFlatten: boolean = true;
    for (const property of schema.properties) {
      if (property.serializedName === "properties") {
        propertiesProperty = property;
        canFlatten = false;
        continue;
      }

      canFlatten = canFlatten && this.canFlatten(property.schema);
    }

    // Schemas where all properties are non-objects can be flattened into multi-parameter commands
    if (canFlatten) {
      for (const property of schema.properties) {
        keyword.addPropertyParameter(property.serializedName, this.getParameterFromSchema(property.schema));
      }

      return;
    }

    // Otherwise, we begin our recursive descent into the keyword structure

    // Most schemas have a "properties" property that contains all the subelements
    if (propertiesProperty) {
      // Add all other properties as parameters
      for (const property of schema.properties) {
        if (property === propertiesProperty) {
          continue;
        }

        keyword.addParameter(property.serializedName, this.getParameterFromSchema(property.schema));
      }

      // Create the body with sub keywords
      const propertiesSchema = <ObjectSchema>propertiesProperty.schema;
      this.addKeywordBodyFromSchema(keyword, propertiesSchema.properties);

      return;
    }

    // We have no "properties" property, but build a body keyword anyway
    this.addKeywordBodyFromSchema(keyword, schema.properties);
  }

  private addKeywordBodyFromSchema(keyword: KeywordBuilder, properties: Property[] | undefined) {
    if (!properties) {
      return;
    }

    const body: Record<string, KeywordPointer> = {};
    this.addKeywordsToTable(body, properties);
    keyword.addBody(body);
  }

  private canFlatten(schema: Schema): boolean {
    switch (schema.type) {
      case SchemaType.Object:
      case SchemaType.Array:
        return false;

      default:
        return true;
    }
  }

  private getParameterFromSchema(schema: Schema): IParameter {
    switch (schema.type) {
      case SchemaType.String:
        return this.getStringParameter(<StringSchema>schema);

      case SchemaType.Integer:
        return this.getIntParameter(<NumberSchema>schema);

      case SchemaType.Boolean:
        return this.getBooleanParameter(<BooleanSchema>schema);

      case SchemaType.Choice:
        return this.getChoiceParameter(<ChoiceSchema>schema);
    }

    return {};
  }

  private getStringParameter(schema: StringSchema): IStringParameter {
    return {
      type: "string",
      pattern: schema.pattern,
    };
  }

  private getIntParameter(schema: NumberSchema): IIntParameter {
    return {
      type: "int",
    }
  }

  private getChoiceParameter(schema: ChoiceSchema): IParameter {
    return {
      enum: schema.choices.map(v => v.value),
    };
  }

  private getBooleanParameter(schema: BooleanSchema): IParameter {
    return {
      type: "bool",
    };
  }
}

class ResourceSchemaCollectionBuilder {
  private resourceProviders: Record<string, Record<string, ResourceProviderBuilder>>;

  constructor(private debug: boolean) {
    this.resourceProviders = {};
  }

  public async writeToDir(dirPath: string): Promise<void> {
    dirPath = await ensureDirExists(dirPath);
    const fileWrites: Promise<void>[] = [];
    for (const providerVersion of Object.values(this.resourceProviders)) {
      for (const provider of Object.values(providerVersion)) {
        fileWrites.push(provider.writeToDir(dirPath));
      }
    }
    await Promise.all(fileWrites);
  }

  public addResourceSchema(
    providerName: string,
    apiVersion: string,
    resourceType: string,
    resourceSchema: ObjectSchema) {

    if (!this.resourceProviders.hasOwnProperty(providerName)) {
      this.resourceProviders[providerName] = {};
    }

    if (!this.resourceProviders[providerName].hasOwnProperty(apiVersion)) {
      this.resourceProviders[providerName][apiVersion] = new ResourceProviderBuilder(providerName, apiVersion);
    }

    this.resourceProviders[providerName][apiVersion].addResource(resourceType, resourceSchema);
  }
}

export async function generator(host: Host) {
  const debug = (await host.GetValue("debug")) || false;

  try {
    // get the code model from the core
    const session = await startSession<CodeModel>(
      host,
      undefined,
      codeModelSchema,
      "code-model-v4"
    );

    //let text = "";
    const resources = new ResourceSchemaCollectionBuilder(debug);

    for (const group of values(session.model.operationGroups)) {
      for (const operation of values(group.operations)) {
        for (const request of values(operation.requests)) {
          if (request.protocol.http?.method === "put") {
            const schema = <ObjectSchema>request?.parameters?.[0].schema;

            const resourceType = getResourceTypeFromPath(request.protocol.http.path);
            const apiVersion: string = schema.apiVersions && schema.apiVersions[0].version || '*';

            resources.addResourceSchema(resourceType.namespace, apiVersion, resourceType.name, schema);

            for (const parent of values(schema.parents?.all)) {
              // parent is one of the parent schemas
              // parent.language.default.name
            }
          }
        }
      }
    }

    await resources.writeToDir("./out");

    /*
    for (const each of values(session.model.schemas.objects)) {
      text = text + `schema: ${each.language.sputnik?.name}\n`;
    }

    // example: output a generated text file
    host.WriteFile(
      "sputnik-sample.txt",
      text,
      undefined,
      "source-file-sputnik"
    );
    */
  } catch (E) {
    if (debug) {
      console.error(`${__filename} - FAILURE  ${JSON.stringify(E)} ${E.stack}`);
    }
    throw E;
  }
}

async function ensureDirExists(dirPath: string): Promise<string> {
  if (!path.isAbsolute(dirPath)) {
    dirPath = path.join(process.cwd(), dirPath);
  }

  if (!await fileExists(dirPath)) {
    console.error(`Creating directory '${dirPath}'`);
    await fsPromises.mkdir(dirPath);
  }

  return dirPath;
}

async function fileExists(path: string): Promise<boolean> {
  try {
    await fsPromises.access(path);
    return true;
  } catch {
    return false;
  }
}

interface IResourceType {
  namespace: string,
  name: string
}

function getResourceTypeFromPath(path: string): IResourceType {
  // Parse the namespace
  let start: number = path.indexOf('/', path.indexOf('providers')) + 1;
  let end: number = path.indexOf('/', start);
  const namespace = path.substring(start, end);

  // Now get the first part of the resource type
  const nameParts: string[] = [];
  start = end + 1;
  end = path.indexOf('/', start) - 1;
  nameParts.push(path.substring(start, end));

  let nextTemplateEnd = path.indexOf('}/', end);
  while (nextTemplateEnd > 0) {
    start = nextTemplateEnd + 2;
    end = path.indexOf('/', start);
    if (end < 0) {
      end = path.length;
    }
    nameParts.push(path.substring(start, end));
    nextTemplateEnd = path.indexOf('}/', end);
  }

  return {
    namespace,
    name: nameParts.join('/'),
  };
}

