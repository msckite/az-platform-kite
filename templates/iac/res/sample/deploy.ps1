
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
[OutputType([psobject])]
param (
    [Parameter(
        HelpMessage = "Required. Environment code to determine the parameter file for deployment. Default value is taken from environment variable 'ENV_CODE'.")]
    [ValidateSet('dev', 'stg', 'prd')]
    [string]$EnvCode = "$($env:ENV_CODE)",

    [Parameter(
        HelpMessage = "Required. Azure subscription ID to deploy the resource group. Default value is taken from environment variable 'AZURE_SUBSCRIPTION_ID'.")]
    [string]$SubscriptionId = "$($env:AZURE_SUBSCRIPTION_ID)",

    [Parameter(
        HelpMessage = "Optional. Name of the resource group used to store the managed resources. Default value is taken from environment variable 'RESOURCE_GROUP_NAME'.")]
    [string]$ResourceGroupName = "$($env:RESOURCE_GROUP_NAME)",

    [Parameter(
        HelpMessage = "Required. Name of the deployment stack. Default value is taken from environment variable 'DEPLOYMENT_STACK_NAME'.")]
    [string]$DeploymentStackName = "$($env:DEPLOYMENT_STACK_NAME)",

    [Parameter(
        HelpMessage = "Optional. Action to take on unmanaged resources. Default is 'DeleteAll'.")]
    [ValidateSet('DetachAll', 'DeleteResources', 'DeleteAll')]
    [string]$ActionOnUnmanage = 'DetachAll',

    [Parameter(
        HelpMessage = "Optional. Deny settings mode for the deployment. Default is 'None'.")]
    [ValidateSet('None', 'DenyDelete', 'DenyWriteAndDelete')]
    [string]$DenySettingsMode = 'None',

    [Parameter(
        HelpMessage = 'Optional. Path to the Bicep template file.')]
    [string]$TemplateFile = 'iac/res/sample/main.bicep',

    [Parameter(
        HelpMessage = "Optional. Path to the Bicep parameter file. Environment value is taken from environment variable. For example, if 'EnvCode' is 'dev', the default parameter file will be 'iac/ptn/sample/params/dev.bicepparam'.")]
    [string]$TemplateParameterFile = "iac/res/sample/params/$($EnvCode).bicepparam"
)

begin {

    $params = [ordered]@{
        EnvCode               = $EnvCode
        SubscriptionId        = $SubscriptionId
        ResourceGroupName     = $ResourceGroupName
        DeploymentStackName   = $DeploymentStackName
        ActionOnUnmanage      = $ActionOnUnmanage
        DenySettingsMode      = $DenySettingsMode
        TemplateFile          = $TemplateFile
        TemplateParameterFile = $TemplateParameterFile
    } | ConvertTo-Json -Depth 3

    Write-Verbose "[Enter]: $($MyInvocation.MyCommand.Name) with parameters: $params"
}

process {
    try {
        # Initialize variables
        $ctx = $null
        $ctxInfo = $null
        $stack = $null
        $stackInput = $null
        $result = $null

        # HACK: Explicitly set -WhatIf:$false, for switching targeted subscription in the context.
        Set-AzContext -TenantId (Get-AzContext).Tenant.Id -SubscriptionId $SubscriptionId -WhatIf:$false | Out-Null

        $ctx = Get-AzContext
        $ctxInfo = [ordered]@{
            Account      = $ctx.Account.Id
            Tenant       = $ctx.Tenant.Id
            Subscription = $ctx.Subscription.Name
        }
        Write-Verbose "Call deployment with context: $($ctxInfo | ConvertTo-Json -Depth 3)"

        $stackInput = @{
            Name                  = $DeploymentStackName
            ResourceGroupName     = $ResourceGroupName
            TemplateFile          = $TemplateFile
            TemplateParameterFile = $TemplateParameterFile
            ActionOnUnmanage      = $ActionOnUnmanage
            DenySettingsMode      = $DenySettingsMode
            Verbose               = $VerbosePreference
            WhatIf                = $WhatIfPreference
        }

        $stack = Get-AzResourceGroupDeploymentStack `
            -ResourceGroupName $ResourceGroupName `
            -Name $DeploymentStackName `
            -ErrorAction SilentlyContinue

        $stackDeployName = "$($DeploymentStackName)-$((Get-Date -Format 'yyyyMMddHHmmss')[0..11] -join '')"

        if (-not $stack) {
            if ($WhatIfPreference) {
                # Compile Bicep to get resource preview
                try {
                    $bicepOutput = az bicep build --file $TemplateFile --stdout | ConvertFrom-Json -AsHashtable
                } catch {
                    throw $_
                }

                # Create synthetic result object
                $result = [PSCustomObject]@{
                    Id         = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"
                    Name       = $ResourceGroupName
                    Type       = 'Microsoft.Resources/resourceGroups'
                    Properties = [PSCustomObject]@{
                        Changes = [PSCustomObject]@{
                            ResourceChanges = @(
                                [PSCustomObject]@{
                                    Id                = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Resources/deploymentStacks/$DeploymentStackName"
                                    Type              = 'Microsoft.Resources/deploymentStacks'
                                    ChangeType        = 'create'
                                    ChangeCertainty   = 'definite'
                                    Resources         = ($bicepOutput.resources.Keys | Sort-Object)
                                    ResourcesCount    = $bicepOutput.resources.Count
                                    ProvisioningState = 'accepted'
                                }
                            )
                        }
                    }
                }

            } else {
                $result = New-AzResourceGroupDeploymentStack @stackInput
            }
        } else {
            if ($WhatIfPreference) {
                $stackWhatIfInput = $stackInput

                $stackWhatIfInput['Name'] = $stackDeployName
                $stackWhatIfInput['WhatIf'] = $false

                $stackWhatIfInput += @{
                    StackResourceId   = $stack.id
                    RetentionInterval = 'PT3H'
                }
                $result = New-AzResourceGroupDeploymentStackWhatIfResult @stackWhatIfInput
            } else {
                $result = Set-AzResourceGroupDeploymentStack @stackInput
            }
        }

        Write-Output -InputObject $result

    } catch {
        throw $_
    }
}

end {
    Write-Verbose "[Exit]: $($MyInvocation.MyCommand.Name)"
}
