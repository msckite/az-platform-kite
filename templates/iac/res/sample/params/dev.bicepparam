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
  notes: 'This resource group is critical for the Be-Inc AI Services and should not be deleted.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
