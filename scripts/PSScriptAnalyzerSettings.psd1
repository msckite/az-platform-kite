# PSScriptAnalyzer settings for this repository, used by the PowerShell extension for script analysis
# (powershell.scriptAnalysis.settingsPath in .vscode/settings.json) and on the command line:
#
#   Invoke-ScriptAnalyzer -Path . -Recurse -Settings ./scripts/PSScriptAnalyzerSettings.psd1
#   Invoke-Formatter -ScriptDefinition $script -Settings ./scripts/PSScriptAnalyzerSettings.psd1
#
# All default analysis rules run. The formatting rules below are configured exactly like the
# powershell.codeFormatting.* settings in .vscode/settings.json, so analysis never reports code that
# format-on-save produced. Keep both in sync when changing either.
@{
    Severity     = @('Error', 'Warning', 'Information')

    # files.encoding: utf8 in .vscode/settings.json saves PowerShell files without a BOM. The module requires
    # PowerShell 7 (PowerShellVersion = '7.0'), which reads UTF-8 without a BOM correctly, so this rule would only
    # report every non-ASCII file (e.g. the '™' in the module manifests) after each save.
    ExcludeRules = @('PSUseBOMForUnicodeEncodedFile')

    Rules        = @{
        # powershell.codeFormatting.preset: OTBS (opening brace on the same line)
        PSPlaceOpenBrace                          = @{
            Enable             = $true
            OnSameLine         = $true
            NewLineAfter       = $true
            IgnoreOneLineBlock = $true
        }

        # powershell.codeFormatting.newLineAfterCloseBrace: false (OTBS: '} else {')
        PSPlaceCloseBrace                         = @{
            Enable             = $true
            NewLineAfter       = $false
            IgnoreOneLineBlock = $true
            NoEmptyLineBefore  = $false
        }

        # [powershell] editor.tabSize: 4 and editor.insertSpaces: true;
        # powershell.codeFormatting.pipelineIndentationStyle: IncreaseIndentationForFirstPipeline
        PSUseConsistentIndentation                = @{
            Enable              = $true
            Kind                = 'space'
            IndentationSize     = 4
            PipelineIndentation = 'IncreaseIndentationForFirstPipeline'
        }

        # powershell.codeFormatting.whitespaceBetweenParameters: true (CheckParameter);
        # powershell.codeFormatting.trimWhitespaceAroundPipe: true (CheckPipeForRedundantWhitespace);
        # the remaining checks are the extension's whitespace defaults
        PSUseConsistentWhitespace                 = @{
            Enable                                  = $true
            CheckInnerBrace                         = $true
            CheckOpenBrace                          = $true
            CheckOpenParen                          = $true
            CheckOperator                           = $true
            CheckPipe                               = $true
            CheckPipeForRedundantWhitespace         = $true
            CheckSeparator                          = $true
            CheckParameter                          = $true
            IgnoreAssignmentOperatorInsideHashTable = $true
        }

        # powershell.codeFormatting.alignPropertyValuePairs (extension default: true)
        PSAlignAssignmentStatement                = @{
            Enable         = $true
            CheckHashtable = $true
        }

        # powershell.codeFormatting.useCorrectCasing: true
        PSUseCorrectCasing                        = @{
            Enable = $true
        }

        # powershell.codeFormatting.autoCorrectAliases: true
        PSAvoidUsingCmdletAliases                 = @{
            Enable = $true
        }

        # powershell.codeFormatting.useConstantStrings: true (single quotes for strings without expansion)
        PSAvoidUsingDoubleQuotesForConstantString = @{
            Enable = $true
        }
    }
}
