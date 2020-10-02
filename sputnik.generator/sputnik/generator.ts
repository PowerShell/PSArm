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
    private apiVersion: string,
    private debug: boolean) {

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
    if (this.debug) {
      log(`Writing schema to '${filePath}'`);
    }
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
    const properties = this.getAllProperties(schema);

    if (!properties) {
      return;
    }

    this.addKeywordsToTable(table, properties);
  }

  private getAllProperties(schema: ObjectSchema): Property[] | undefined {
    const ownProperties: Property[] | undefined = this.getPropertiesPropertyProperties(schema);
    const parentProperties: Property[] | undefined = schema.parents?.all
      .map(p => {
        if (p.type !== SchemaType.Object) {
          return undefined;
        }

        return this.getPropertiesPropertyProperties(<ObjectSchema>p);
      })
      .reduce((acc, ps) => ps ? acc?.concat(ps) : acc, []);

    if (!ownProperties) {
      return parentProperties;
    }

    if (parentProperties) {
      ownProperties.concat(parentProperties);
    }

    return ownProperties;
  }

  private getPropertiesPropertyProperties(schema: ObjectSchema): Property[] | undefined {
    if (!schema.properties) {
      return undefined;
    }

    const propertiesProperties: Property[] = [];
    for (const property of schema.properties) {
      // Get the actual "properties" properties
      if (property.serializedName === "properties") {
        if (property.schema.type !== SchemaType.Object) {
          log(`WARNING: Expected "properties" property to be an object schema, but instead got '${property.schema.type}'`);
          continue;
        }
        const properties = (<ObjectSchema>property.schema).properties;
        if (properties) {
          propertiesProperties.push(...properties);
        }
        continue;
      }

      // Now look for flattened properties.
      // These will be inlined into the containing object, so we add them directly
      if (property.flattenedNames && property.flattenedNames.find(e => e === "properties")) {
        propertiesProperties.push(property);
      }
    }
    return propertiesProperties;
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
    const { properties, propertyProperties } = this.collectProperties(schema);

    const bodyProperties: Property[] = [];

    for (const property of (properties || [])) {
      if (this.canFlatten(property.schema)) {
        keyword.addParameter(this.getPropertyNameForDsl(property), this.getParameterFromSchema(property.schema));
        continue;
      }
      bodyProperties.push(property);
    }

    for (const pProperty of (propertyProperties || [])) {
      if (this.canFlatten(pProperty.schema)) {
        keyword.addPropertyParameter(this.getPropertyNameForDsl(pProperty), this.getParameterFromSchema(pProperty.schema));
        continue;
      }
      bodyProperties.push(pProperty);
    }

    if (bodyProperties.length > 0) {
      this.addKeywordBodyFromSchema(keyword, bodyProperties);
    }
  }

  private collectProperties(schema: ObjectSchema): { properties?: Property[], propertyProperties?: Property[] } {
    const properties: Property[] = [];
    const propertyProperties: Property[] = [];

    this.collectPropertiesFromPropertyList(properties, propertyProperties, schema.properties);

    if (schema.parents) {
      for (const parent of schema.parents.all) {
        this.collectPropertiesFromPropertyList(properties, propertyProperties, (<ObjectSchema>parent).properties);
      }
    }

    return {
      properties: properties.length > 0 ? properties : undefined,
      propertyProperties: propertyProperties.length > 0 ? propertyProperties : undefined,
    };
  }

  private collectPropertiesFromPropertyList(acc: Property[], ppAcc: Property[], properties?: Property[]) {
    if (!properties) {
      return;
    }

    for (const property of properties) {
      // "properties" handling
      if (property.serializedName === "properties") {
        const pProperties = (<ObjectSchema>property.schema).properties;
        if (pProperties) {
          for (const pProperty of pProperties) {
            ppAcc.push(pProperty);
          }
        }
        continue;
      }

      acc.push(property);
    }
  }

  private addKeywordBodyFromSchema(keyword: KeywordBuilder, properties: Property[] | undefined) {
    if (!properties) {
      return;
    }

    const body: Record<string, KeywordPointer> = {};
    this.addKeywordsToTable(body, properties);
    keyword.addBody(body);
  }

  private getPropertyNameForDsl(property: Property): string {
    return property.serializedName;
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
      this.resourceProviders[providerName][apiVersion] = new ResourceProviderBuilder(providerName, apiVersion, this.debug);
    }

    this.resourceProviders[providerName][apiVersion].addResource(resourceType, resourceSchema);
  }
}

function log(message: string) {
  console.error(`PSARM-GEN: ${message}`);
}

async function getTagVersion(host: Host): Promise<string | undefined> {
  const tag: string | undefined = (await host.GetValue("tag")) || undefined;

  if (!tag) {
    return undefined;
  }

  return fixVersion(tag.substr(8));
}

function fixVersion(givenVersion: string | undefined): string | undefined {
  if (!givenVersion) {
    return undefined;
  }

  const result = /^\d{4}-\d{2}(-\d{2})?(-preview)?$/.exec(givenVersion);

  // No match
  if (!result) {
    return undefined;
  }

  const fullVersion = result[0];

  // Second group matches, so we have a full date
  if (result[1]) {
    return fullVersion;
  }

  // There's a preview tag, so we need to add the 01 before that
  if (result[2]) {
    return `${fullVersion.substr(0, 7)}-01-preview`;
  }

  return `${fullVersion}-01`;
}

export async function generator(host: Host) {
  const debug = (await host.GetValue("debug")) || false;
  const tagVersion = await getTagVersion(host);

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

            if (!schema) {
              continue;
            }

            const resourceType = getResourceTypeFromPath(request.protocol.http.path);
            const givenVersion = schema.apiVersions && schema.apiVersions[0].version;

            const apiVersion = fixVersion(givenVersion) || tagVersion || '*';

            if (debug) {
              log(`Generating schema for '${resourceType.namespace}/${resourceType.name}-${apiVersion}'`);
            }

            resources.addResourceSchema(resourceType.namespace, apiVersion, resourceType.name, schema);
          }
        }
      }
    }

    const outputDir = await host.GetValue("output-folder") || "./out";
    if (debug) {
      log(`Writing generated files to ${outputDir}`);
    }
    await resources.writeToDir(outputDir);

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
      log(`${__filename} - FAILURE  ${JSON.stringify(E)} ${E.stack}`);
    }
    throw E;
  }
}

async function ensureDirExists(dirPath: string): Promise<string> {
  if (!path.isAbsolute(dirPath)) {
    dirPath = path.join(process.cwd(), dirPath);
  }

  if (!await fileExists(dirPath)) {
    log(`Creating directory '${dirPath}'`);
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
  end = path.indexOf('/', start);
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
