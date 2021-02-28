
# Copyright (c) Microsoft Corporation.

function add
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call add -Arguments $args
}

function and
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call and -Arguments $args
}

function array
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call array -Arguments $args
}

function base64
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call base64 -Arguments $args
}

function base64ToJson
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call base64ToJson -Arguments $args
}

function base64ToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call base64ToString -Arguments $args
}

function bool
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call bool -Arguments $args
}

function coalesce
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call coalesce -Arguments $args
}

function concat
{
   Call concat -Arguments $args
}

function contains
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call contains -Arguments $args
}

function copyIndex
{
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call copyIndex -Arguments $args
}

function createArray
{
   Call createArray -Arguments $args
}

function createObject
{
   Call createObject -Arguments $args
}

function dataUri
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call dataUri -Arguments $args
}

function dataUriToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call dataUriToString -Arguments $args
}

function dateTimeAdd
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call dateTimeAdd -Arguments $args
}

function deployment
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call deployment -Arguments $args
}

function div
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call div -Arguments $args
}

function empty
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call empty -Arguments $args
}

function endsWith
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call endsWith -Arguments $args
}

function environment
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call environment -Arguments $args
}

function equals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call equals -Arguments $args
}

function extensionResourceId
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   Call extensionResourceId -Arguments $args
}

function false
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call false -Arguments $args
}

function first
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call first -Arguments $args
}

function float
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call float -Arguments $args
}

function format
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call format -Arguments $args
}

function greater
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call greater -Arguments $args
}

function greaterOrEquals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call greaterOrEquals -Arguments $args
}

function guid
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call guid -Arguments $args
}

function if
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call if -Arguments $args
}

function indexOf
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call indexOf -Arguments $args
}

function int
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call int -Arguments $args
}

function intersection
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call intersection -Arguments $args
}

function json
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call json -Arguments $args
}

function last
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call last -Arguments $args
}

function lastIndexOf
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call lastIndexOf -Arguments $args
}

function length
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call length -Arguments $args
}

function less
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call less -Arguments $args
}

function lessOrEquals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call lessOrEquals -Arguments $args
}

function list
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call list -Arguments $args
}

function listAccountSas
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call listAccountSas -Arguments $args
}

function listAdminKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listAdminKeys -Arguments $args
}

function listAuthKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listAuthKeys -Arguments $args
}

function listCallbackUrl
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listCallbackUrl -Arguments $args
}

function listChannelWithKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listChannelWithKeys -Arguments $args
}

function listClusterAdminCredential
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listClusterAdminCredential -Arguments $args
}

function listConnectionStrings
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listConnectionStrings -Arguments $args
}

function listCredentials
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listCredentials -Arguments $args
}

function listCredential
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listCredential -Arguments $args
}

function listKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listKeys -Arguments $args
}

function listKeyValue
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call listKeyValue -Arguments $args
}

function listPackage
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listPackage -Arguments $args
}

function listQueryKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listQueryKeys -Arguments $args
}

function listSecrets
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listSecrets -Arguments $args
}

function listServiceSas
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call listServiceSas -Arguments $args
}

function listSyncFunctionTriggerStatus
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call listSyncFunctionTriggerStatus -Arguments $args
}

function max
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call max -Arguments $args
}

function min
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call min -Arguments $args
}

function mod
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call mod -Arguments $args
}

function mul
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call mul -Arguments $args
}

function newGuid
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call newGuid -Arguments $args
}

function not
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call not -Arguments $args
}

function null
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call null -Arguments $args
}

function or
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call or -Arguments $args
}

function padLeft
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call padLeft -Arguments $args
}

function parameters
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call parameters -Arguments $args
}

function providers
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call providers -Arguments $args
}

function range
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call range -Arguments $args
}

function reference
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call reference -Arguments $args
}

function replace
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call replace -Arguments $args
}

function resourceGroup
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call resourceGroup -Arguments $args
}

function resourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call resourceId -Arguments $args
}

function skip
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call skip -Arguments $args
}

function split
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call split -Arguments $args
}

function startsWith
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call startsWith -Arguments $args
}

function string
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call string -Arguments $args
}

function sub
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call sub -Arguments $args
}

function subscription
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call subscription -Arguments $args
}

function subscriptionResourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call subscriptionResourceId -Arguments $args
}

function substring
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   Call substring -Arguments $args
}

function take
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call take -Arguments $args
}

function tenantResourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call tenantResourceId -Arguments $args
}

function toLower
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call toLower -Arguments $args
}

function toUpper
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call toUpper -Arguments $args
}

function trim
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call trim -Arguments $args
}

function true
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   Call true -Arguments $args
}

function union
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   Call union -Arguments $args
}

function uniqueString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   Call uniqueString -Arguments $args
}

function uri
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   Call uri -Arguments $args
}

function uriComponent
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call uriComponent -Arguments $args
}

function uriComponentToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call uriComponentToString -Arguments $args
}

function utcNow
{
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call utcNow -Arguments $args
}

function variables
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   Call variables -Arguments $args
}
