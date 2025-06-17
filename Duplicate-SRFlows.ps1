#Copyright (c) 2014, Unify Solutions Pty Ltd
#All rights reserved.
#
#Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
#* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
#* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
#
#THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
#IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
#OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

###
### Duplicate-SRFlows.ps1
###
### Copy the flow rules from one SynchronizationRule object to another.
### Will work when SRs target different MAs, as long as schema is the same.
### Uploads rules one at a time in case some fail.
###
### USAGE: .\Duplicate-SRFlows.ps1 -SourceSR "<Source SR Display Name>" -TargetSR "<Target SR Display Name>"
###
### Notes:
###  - Uses the FIMPowerShell Function library from http://technet.microsoft.com/en-us/library/ff720152(v=ws.10).aspx
###

PARAM($SourceSR,$TargetSR)

. C:\FIM\Scripts\FIMFunctions.ps1


$ObjectType="SynchronizationRule"

[string]$filter = "/" + $ObjectType + "[DisplayName = '" + $SourceSR + "']"
$SourceObj = export-fimconfig -customConfig $filter -OnlyBaseResources
$hashObj = ConvertResourceToHashtable $SourceObj

[string]$filter = "/" + $ObjectType + "[DisplayName = '" + $TargetSR + "']"
$TargetObj = export-fimconfig -customConfig $filter -OnlyBaseResources

foreach ($flow in $hashObj.InitialFlow)
{
    # Add each flow rule seperately in case some fail
    $importObj = ModifyImportObject -ObjectType $ObjectType -TargetIdentifier $TargetObj.ResourceManagementObject.ObjectIdentifier
    AddMultiValue -ImportObject $importObj -AttributeName "InitialFlow" -NewAttributeValue $flow
    $importObj.Changes
    Import-FIMConfig -ImportObject $importObj
}
foreach ($flow in $hashObj.PersistentFlow)
{
    # Add each flow rule seperately in case some fail
    $importObj = ModifyImportObject -ObjectType $ObjectType -TargetIdentifier $TargetObj.ResourceManagementObject.ObjectIdentifier
    AddMultiValue -ImportObject $importObj -AttributeName "PersistentFlow" -NewAttributeValue $flow
    $importObj.Changes
    Import-FIMConfig -ImportObject $importObj
}
