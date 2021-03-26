
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function add
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall add -Arguments $args
}

function and
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall and -Arguments $args
}

function array
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall array -Arguments $args
}

function base64
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall base64 -Arguments $args
}

function base64ToJson
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall base64ToJson -Arguments $args
}

function base64ToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall base64ToString -Arguments $args
}

function bool
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall bool -Arguments $args
}

function coalesce
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall coalesce -Arguments $args
}

function concat
{
   RawCall concat -Arguments $args
}

function contains
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall contains -Arguments $args
}

function copyIndex
{
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall copyIndex -Arguments $args
}

function createArray
{
   RawCall createArray -Arguments $args
}

function createObject
{
   RawCall createObject -Arguments $args
}

function dataUri
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall dataUri -Arguments $args
}

function dataUriToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall dataUriToString -Arguments $args
}

function dateTimeAdd
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall dateTimeAdd -Arguments $args
}

function deployment
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall deployment -Arguments $args
}

function div
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall div -Arguments $args
}

function empty
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall empty -Arguments $args
}

function endsWith
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall endsWith -Arguments $args
}

function environment
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall environment -Arguments $args
}

function equals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall equals -Arguments $args
}

function extensionResourceId
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   RawCall extensionResourceId -Arguments $args
}

function false
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall false -Arguments $args
}

function first
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall first -Arguments $args
}

function float
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall float -Arguments $args
}

function format
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall format -Arguments $args
}

function greater
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall greater -Arguments $args
}

function greaterOrEquals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall greaterOrEquals -Arguments $args
}

function guid
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall guid -Arguments $args
}

function if
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall if -Arguments $args
}

function indexOf
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall indexOf -Arguments $args
}

function int
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall int -Arguments $args
}

function intersection
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall intersection -Arguments $args
}

function json
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall json -Arguments $args
}

function last
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall last -Arguments $args
}

function lastIndexOf
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall lastIndexOf -Arguments $args
}

function length
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall length -Arguments $args
}

function less
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall less -Arguments $args
}

function lessOrEquals
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall lessOrEquals -Arguments $args
}

function list
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall list -Arguments $args
}

function listAccountSas
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall listAccountSas -Arguments $args
}

function listAdminKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listAdminKeys -Arguments $args
}

function listAuthKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listAuthKeys -Arguments $args
}

function listRawCallbackUrl
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listRawCallbackUrl -Arguments $args
}

function listChannelWithKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listChannelWithKeys -Arguments $args
}

function listClusterAdminCredential
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listClusterAdminCredential -Arguments $args
}

function listConnectionStrings
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listConnectionStrings -Arguments $args
}

function listCredentials
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listCredentials -Arguments $args
}

function listCredential
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listCredential -Arguments $args
}

function listKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listKeys -Arguments $args
}

function listKeyValue
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall listKeyValue -Arguments $args
}

function listPackage
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listPackage -Arguments $args
}

function listQueryKeys
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listQueryKeys -Arguments $args
}

function listSecrets
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listSecrets -Arguments $args
}

function listServiceSas
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall listServiceSas -Arguments $args
}

function listSyncFunctionTriggerStatus
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall listSyncFunctionTriggerStatus -Arguments $args
}

function max
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall max -Arguments $args
}

function min
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall min -Arguments $args
}

function mod
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall mod -Arguments $args
}

function mul
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall mul -Arguments $args
}

function newGuid
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall newGuid -Arguments $args
}

function not
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall not -Arguments $args
}

function null
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall null -Arguments $args
}

function or
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall or -Arguments $args
}

function padLeft
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall padLeft -Arguments $args
}

function parameters
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall parameters -Arguments $args
}

function providers
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall providers -Arguments $args
}

function range
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall range -Arguments $args
}

function reference
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall reference -Arguments $args
}

function replace
{
   if ($args.Count -lt 3){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall replace -Arguments $args
}

function resourceGroup
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall resourceGroup -Arguments $args
}

function resourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall resourceId -Arguments $args
}

function skip
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall skip -Arguments $args
}

function split
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall split -Arguments $args
}

function startsWith
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall startsWith -Arguments $args
}

function string
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall string -Arguments $args
}

function sub
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall sub -Arguments $args
}

function subscription
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall subscription -Arguments $args
}

function subscriptionResourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall subscriptionResourceId -Arguments $args
}

function substring
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 3){ throw 'Exceeded maximum parameter count' }
   RawCall substring -Arguments $args
}

function take
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall take -Arguments $args
}

function tenantResourceId
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall tenantResourceId -Arguments $args
}

function toLower
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall toLower -Arguments $args
}

function toUpper
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall toUpper -Arguments $args
}

function trim
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall trim -Arguments $args
}

function true
{
   if ($args.Count -gt 0){ throw 'Exceeded maximum parameter count' }
   RawCall true -Arguments $args
}

function union
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   RawCall union -Arguments $args
}

function uniqueString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   RawCall uniqueString -Arguments $args
}

function uri
{
   if ($args.Count -lt 2){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 2){ throw 'Exceeded maximum parameter count' }
   RawCall uri -Arguments $args
}

function uriComponent
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall uriComponent -Arguments $args
}

function uriComponentToString
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall uriComponentToString -Arguments $args
}

function utcNow
{
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall utcNow -Arguments $args
}

function variables
{
   if ($args.Count -lt 1){ throw 'Not enough parameters provided' }
   if ($args.Count -gt 1){ throw 'Exceeded maximum parameter count' }
   RawCall variables -Arguments $args
}
