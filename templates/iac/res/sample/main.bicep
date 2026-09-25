metadata name = 'Sample Workload Deployment'
metadata description = 'This Bicep template deploys a sample workload in Azure.'
metadata owner = 'platform-engineers'

// ---------- //
// PARAMETERS //
// ---------- //

@description('Required. The environment code for the deployment. This is used to generate resource names and tags.')
param envCode string

@description('Required. A unique identifier for the deployment. This is used to generate resource names and tags.')
param uniqueId string

@description('Optional. A short name for the service. This is used to generate resource names and tags.')
param serviceShort string = 'workload'

@description('Optional. An object with names of the resources to be deployed.')
param resourceNames object = {
  // Storage account names allow only lowercase letters/numbers and must be 3-24 characters.
  storageAccountName: toLower('st${serviceShort}${uniqueId}${envCode}')
}

@description('Optional. Location deployment metadata.')
param location string = resourceGroup().location

@description('Optional. An object of key/value pairs to be appended as tags to the resource.')
param tags object = {
  location: location
  owner: 'platform-engineers'
  role: 'validation'
}

@description('Optional. Enable/Disable usage telemetry for AVM module.')
param enableTelemetry bool = false

@description('Optional. Storage account redundancy. Higher redundancy costs more, so this typically increases per environment (e.g. dev: LRS, stg: ZRS, prd: GRS/RAGRS).')
@allowed([
  'Standard_LRS'
  'Standard_ZRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param skuName string = 'Standard_LRS'

@description('''Optional. The lock settings of all resources in the resource group.
- `name` - The name of the lock.
- `kind` - The lock settings of the service which can be CanNotDelete, ReadOnly, or None.
- `notes` - Notes about this lock.
''')
param lock lockType?

// --------- //
// VARIABLES //
// --------- //

// --------- //
// RESOURCES //
// --------- //

// Lightweight sample resource, sourced from the Azure Verified Modules (AVM) registry.
module storageAccount 'br/public:avm/res/storage/storage-account:0.33.0' = {
  name: '${uniqueString(deployment().name, location)}-storage-account'
  params: {
    name: resourceNames.storageAccountName
    location: location
    tags: tags
    skuName: skuName
    enableTelemetry: enableTelemetry
    lock: lock
  }
}

// ------- //
// OUTPUTS //
// ------- //

@description('The name of the resource group where resources are deployed.')
output resourceGroupName string = resourceGroup().name

@description('The location of the resource group where resources are deployed.')
output resourceGroupLocation string = location

@description('The resource ID of the resource group where resources are deployed.')
output resourceGroupResourceId string = resourceGroup().id

@description('The name of the deployed storage account.')
output storageAccountName string = storageAccount.outputs.name

@description('The resource ID of the deployed storage account.')
output storageAccountResourceId string = storageAccount.outputs.resourceId

// ---------------- //
// TYPE DEFINITIONS //
// ---------------- //

import {
  lockType
} from 'types/main.bicep'
