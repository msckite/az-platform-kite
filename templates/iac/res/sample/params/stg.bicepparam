using '../main.bicep'
extends './base.bicepparam'

// VARIABLES //
var env = 'staging'

// BASE PARAMETERS //
param envCode = 'stg'
param tags = {
  ...base.tags
  environment: env
  criticality: 'medium'
  'cost-center': 'business'
}
param lock = {
  kind: 'None'
  notes: 'This resource group is critical for the Be-Inc AI Services and should not be deleted.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
