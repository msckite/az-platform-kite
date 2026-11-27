using none

// Values shared with the platform configuration
var globalConfig = loadJsonContent('../../../../config/global-config.jsonc')

// BASE PARAMETERS //
param uniqueId = globalConfig.uniqueId
param serviceShort = globalConfig.serviceShort
param location = globalConfig.location
param tags = {
  service: serviceShort
  region: location
  role: 'demo'
  owner: 'platform-engineers'
}
param enableTelemetry = false
