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

type ResourceProviderCollection = Record<string, ResourceProvider>;
type ResourceProvider = Record<string, ResourceVersions>;
type ResourceVersions = Record<string, Resource>;
type Resource = Record<string, IResourceKeyword>

interface IResourceKeyword {
  array?: boolean,
  parameters?: Record<string, IParameter>,
  propertyParameters?: Record<string, IParameter>,
  body?: Record<string, IResourceKeyword>,
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

class Lazy<T> {
  private isCreated: boolean;
  private factory: () => T;
  private result: T | undefined;

  constructor(factory: () => T) {
    this.factory = factory;
    this.isCreated = false;
    this.result = undefined;
  }

  get isValueCreated(): boolean {
    return this.isCreated;
  }

  get value(): T {
    if (!this.isCreated) {
      this.result = this.factory();
    }

    return <T>this.result;
  }
}

class SchemaContext {
  private scopeStack: Set<Schema>[];

  constructor() {
    this.scopeStack = [];
  }

  public pushScope() {
    this.scopeStack.push(new Set());
  }

  public popScope() {
    this.scopeStack.pop();
  }

  public checkDuplicate(schema: Schema): boolean {
    for (let i = this.scopeStack.length - 1; i >= 0; i--) {
      if (this.scopeStack[i].has(schema)) {
        return true;
      }
    }

    this.scopeStack[this.scopeStack.length - 1].add(schema);
    return false;
  }
}

class ResourceSchemaCollectionBuilder {
  private schemas: ResourceProviderCollection;
  private schemaContextTracker: SchemaContext;

  constructor(
    private debug: boolean) {
    this.schemas = {};
    this.schemaContextTracker = new SchemaContext();
  }

  public async writeToDir(dirPath: string) {
    dirPath = await this.ensureDirExists(dirPath);

    for (const provider of Object.keys(this.schemas)) {
      const providerPath = await this.ensureDirExists(path.join(dirPath, provider));
      const providerResouces = this.schemas[provider];
      for (const resourceType of Object.keys(providerResouces)) {
        const resourceVersions = providerResouces[resourceType];
        for (const resourceVersion of Object.keys(resourceVersions)) {
          const fileName = `${resourceType.replace(/\//g, '+')}_${resourceVersion}.json`
          const filePath = path.join(providerPath, fileName);
          if (this.debug) {
            console.error(`Writing ${fileName}`);
          }
          await fsPromises.writeFile(filePath, JSON.stringify(resourceVersions[resourceVersion], null, 2));
        }
      }
    }
  }

  private async ensureDirExists(dirPath: string): Promise<string> {
    if (!path.isAbsolute(dirPath)) {
      dirPath = path.join(process.cwd(), dirPath);
    }

    if (!await this.fileExists(dirPath)) {
      console.error(`Creating directory '${dirPath}'`)
      await fsPromises.mkdir(dirPath);
    }

    return dirPath;
  }

  private async fileExists(filePath: string): Promise<boolean> {
    try {
      await fsPromises.access(filePath);
      return true;
    } catch {
      return false;
    }
  }

  public addResourceSchema(
    namespace: string,
    type: string,
    apiVersion: string,
    resourceSchema: ObjectSchema) {

    if (!hasKey(this.schemas, namespace)) {
      this.schemas[namespace] = {};
    }

    if (!hasKey(this.schemas[namespace], type)) {
      this.schemas[namespace][type] = {};
    }

    this.schemas[namespace][type][apiVersion] = this.getResourceSchema(resourceSchema);
  }

  private getResourceSchema(schema: ObjectSchema): Resource {
    const resource: Resource = {};

    const properties = this.getInnerProperties(schema);

    if (!properties) {
      return resource;
    }

    for (const keywordProperty of properties) {
      this.schemaContextTracker.pushScope();
      const result = this.getKeywordSchema(keywordProperty.language.default.name, keywordProperty.schema);
      this.schemaContextTracker.popScope();
      resource[result.name] = result.keyword;
    }

    return resource;
  }

  private getKeywordSchema(propertyName: string, schema: Schema): { name: string, keyword: IResourceKeyword } {
    switch (schema.type) {
      case SchemaType.Object:
        return {
          name: propertyName,
          keyword: this.getObjectKeywordSchema(<ObjectSchema>schema),
        };

      case SchemaType.Array:
        const arrayResult = this.getKeywordSchema(propertyName, (<ArraySchema>schema).elementType);
        arrayResult.keyword.array = true;
        return arrayResult;

      default:
        return {
          name: propertyName,
          keyword: this.getOtherKeywordSchema(schema),
        };
    }
  }

  private getObjectKeywordSchema(schema: ObjectSchema): IResourceKeyword {
    if (!schema.properties) {
      return {};
    }

    const parameters: Record<string, IParameter> = {};
    const body: Record<string, IResourceKeyword> = {};
    const propertyParameters: Record<string, IParameter> = {};
    for (const property of schema.properties) {
      switch (property.serializedName) {
        case "properties":
          const subproperties = (<ObjectSchema>property.schema).properties;
          if (subproperties) {
            if (subproperties.every(subproperty => this.canFlattenProperty(subproperty))) {
              for (const subproperty of subproperties) {
                propertyParameters[subproperty.language.default.name] = this.getParameterFromProperty(subproperty);
              }
              continue;
            }

            for (const subproperty of subproperties) {
              if (this.schemaContextTracker.checkDuplicate(subproperty.schema)) {
                continue;
              }
              this.schemaContextTracker.pushScope();
              const result = this.getKeywordSchema(subproperty.language.default.name, subproperty.schema);
              this.schemaContextTracker.popScope();
              body[result.name] = result.keyword;
            }
          }
          continue;

        default:
          parameters[property.language.default.name] = this.getParameterFromProperty(property);
          continue;
      }
    }

    return {
      parameters: Object.keys(parameters).length > 0 ? parameters : undefined,
      body: Object.keys(body).length > 0 ? body : undefined,
      propertyParameters: Object.keys(propertyParameters).length > 0 ? propertyParameters : undefined,
    }
  }

  private canFlattenProperty(property: Property): boolean {
    switch (property.schema.type) {
      case SchemaType.Object:
      case SchemaType.Array:
        return false;

      default:
        return true;
    }
  }

  private getInnerProperties(schema: ObjectSchema): Property[] | undefined {
    return (<ObjectSchema>schema.properties?.find(property => property.serializedName == 'properties')?.schema)?.properties;
  }

  private getOtherKeywordSchema(schema: Schema): IResourceKeyword {
    const keyword: IResourceKeyword = {
      propertyParameters: {
        value: this.getParameterFromSchema(schema),
      }
    };
    return keyword;
  }

  private getParameterFromProperty(property: Property): IParameter {
    return this.getParameterFromSchema(property.schema);
  }

  private getParameterFromSchema(schema: Schema) {
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

function hasKey(obj: Object, key: string): boolean {
  return Object.prototype.hasOwnProperty.call(obj, key);
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

            resources.addResourceSchema(resourceType.namespace, resourceType.name, apiVersion, schema);

            for (const parent of values(schema.parents?.all)) {
              // parent is one of the parent schemas
              // parent.language.default.name
            }
          }
        }
      }
    }

    await resources.writeToDir('./out');

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

