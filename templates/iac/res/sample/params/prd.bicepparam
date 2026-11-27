using '../main.bicep'
extends './base.bicepparam'

// VARIABLES //
var env = 'production'

// BASE PARAMETERS //
param envCode = 'prd'
param tags = {
  ...base.tags
  environment: env
  criticality: 'high'
  'cost-center': 'business'
}
param lock = {
  kind: 'None'
  notes: 'Set kind to CanNotDelete to protect these resources.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
