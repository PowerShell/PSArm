
# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# IMPORTANT NOTE

This project uses a git submodule for dependent code. When cloning this repository use `git clone --recursive ...` or perform a `git submodule init ` after the project is cloned.



# AutoRest extension configuration

``` yaml
load-priority: 1000
pipeline-model: v3
```

``` yaml
title: none

pipeline:
  azureresourceschema/imodeler2:
    input: openapi-document/multi-api/identity
    output-artifact: code-model-v1
    scope: azureresourceschema
  azureresourceschema/commonmarker:
    input: imodeler2
    output-artifact: code-model-v1
  azureresourceschema/cm/transform:
    input: commonmarker
    output-artifact: code-model-v1
  azureresourceschema/cm/emitter:
    input: transform
    scope: scope-cm/emitter
  azureresourceschema/generate:
    plugin: azureresourceschema
    input: cm/transform
    output-artifact: source-file-azureresourceschema
  azureresourceschema/transform:
    input: generate
    output-artifact: source-file-azureresourceschema
    scope: scope-transform-string
  azureresourceschema/emitter:
    input: transform
    scope: scope-azureresourceschema/emitter

scope-azureresourceschema/emitter:
  input-artifact: source-file-azureresourceschema
  output-uri-expr: $key

scope-transform-string:
  is-object: false

output-artifact:
- source-file-azureresourceschema

scope-cm/emitter:
  input-artifact: code-model-v1
  is-object: true
  output-uri-expr: |
    "code-model-v1"

scope-cm-yaml/emitter:
  input-artifact: code-model-v1-yaml
  is-object: true
  output-uri-expr: |
    "code-model-v1-yaml"
```
