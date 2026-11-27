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
  notes: 'Set kind to CanNotDelete to protect these resources.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
