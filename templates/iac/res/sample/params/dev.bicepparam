using '../main.bicep'
extends './base.bicepparam'

// VARIABLES //
var env = 'development'

// BASE PARAMETERS //
param envCode = 'dev'
param tags = {
  ...base.tags
  environment: env
  criticality: 'low'
  'cost-center': 'engineering'
}
param lock = {
  kind: 'None'
  notes: 'Set kind to CanNotDelete to protect these resources.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
