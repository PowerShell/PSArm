# AutoRest Sputnik

The Sputnik plugin is used to show a sample plugin and it's parts.
Searching for the word 'Sputnik' in this project will find all the places that need to be changed. 

### Autorest plugin configuration
- Please don't edit this section unless you're re-configuring how the Sputnik extension plugs in to AutoRest
AutoRest needs the below config to pick this up as a plug-in - see https://github.com/Azure/autorest/blob/master/docs/developer/architecture/AutoRest-extension.md

> if the modeler is loaded already, use that one, otherwise grab it.

> Multi-Api Mode
``` yaml
pipeline-model: v3
```

# Pipeline Configuration
``` yaml

modelerfour:
  flatten-models: false
  flatten-payloads: false
  group-parameters: false
  prenamer: false
  merge-response-headers: false
  
use-extension: 
  "@autorest/modelerfour": "4.13.312"

pipeline:
  # generates code
  sputnik:
    input: modelerfour/identity # and the generated c# files

  # extensibility: allow text-transforms after the code gen
  sputnik/text-transform:
    input: sputnik

  # output the files to disk
  sputnik/emitter:
    input: 
      - sputnik/text-transform # this grabs the outputs after the last step.
      
    is-object: false # tell it that we're not putting an object graph out
    output-artifact: source-file-sputnik # the file 'type' that we're outputting.

```
