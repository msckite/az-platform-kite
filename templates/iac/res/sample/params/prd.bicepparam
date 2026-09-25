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
  notes: 'This resource group is critical for the Be-Inc AI Services and should not be deleted.'
}

// PARAMETERS //
param skuName = 'Standard_LRS'
