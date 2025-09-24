<#
THIS CODE AND ANY ASSOCIATED INFORMATION ARE PROVIDED “AS IS” WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
PURPOSE. THE ENTIRE RISK OF USE, INABILITY TO USE, OR RESULTS FROM THE USE OF THIS CODE REMAINS WITH THE USER.

#>


function Execute-RunProfile {   
    param (   
        [Parameter(Mandatory = $true)]   
        [ValidateNotNullOrEmpty()]   
        [string] $MaName,   
           
        [Parameter(Mandatory = $true)]   
        [ValidateNotNullOrEmpty()]   
        [string] $RunProfile,   
           
        [ValidateNotNullOrEmpty()]   
        [string] $ComputerName = '.'   
    )    
   
    Start-Job -Name "$($ComputerName)\$($maName) ($($profileName))" -ArgumentList $MaName,$RunProfile,$ComputerName -ScriptBlock {   
        param($MaName, $RunProfile, $ComputerName)   
           
        $ma = Get-WmiObject -Class "MIIS_ManagementAgent" -Namespace "root\MicrosoftIdentityIntegrationServer" -Filter "Name = '$MaName'" -Computer $ComputerName   
        $started = Get-Date   
        $result = $ma.Execute($RunProfile)   
        $finished = Get-Date   
           
        $ma | Select-Object @{N='MA';E={$MaName}},@{N='RunProfile';E={$RunProfile}},@{N='Result';E={$result.ReturnValue}},@{N='Started';E={$started}},@{N='Finished';E={Get-Date}},@{N='Duration';E={(Get-Date) - $started}},@{N='Details';E={[xml]$ma.RunDetails().ReturnValue}}   
           
        if ($result.ReturnValue -ne 'success') {   
            throw $result.ReturnValue   
        }   
    }   
}  