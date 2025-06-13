function CreateImportObject
{
    PARAM([string]$ObjectType)
    END
    {
        $importObject = New-Object Microsoft.ResourceManagement.Automation.ObjectModel.ImportObject
        $importObject.SourceObjectIdentifier = [System.Guid]::NewGuid().ToString()
        $importObject.ObjectType = $ObjectType
        $importObject
    }
}

function SetSingleValue
{
    PARAM($ImportObject, $AttributeName, $NewAttributeValue, $FullyResolved=1)
    END
    {
        $ImportChange = CreateImportChange -AttributeName $AttributeName -AttributeValue $NewAttributeValue -Operation 1
        $ImportChange.FullyResolved = $FullyResolved
        AddImportChangeToImportObject $ImportChange $ImportObject
    }
}

function AddImportChangeToImportObject
{
    PARAM($ImportChange, $ImportObject)
    END
    {
        if ($ImportObject.Changes -eq $null)
        {
            $ImportObject.Changes = (,$ImportChange)
        }
        else
        {
            $ImportObject.Changes += $ImportChange
        }
    }
}

function CreateImportChange
{
    PARAM($AttributeName, $AttributeValue, $Operation)
    END
    {
        $importChange = New-Object Microsoft.ResourceManagement.Automation.ObjectModel.ImportChange
        $importChange.Operation = $Operation
        $importChange.AttributeName = $AttributeName
        $importChange.AttributeValue = $AttributeValue
        $importChange.FullyResolved = 1
        $importChange.Locale = "Invariant"
        $importChange
    }
}

$Agencies = GCI *.xml
$ObjectType = "Agency"
$SkipAttribs = @("ObjectType","ObjectID","MVObjectID","CreatedTime","Creator")


foreach ($agency in $Agencies)
{
    $hashObj = $Null
    $hashObj = Import-Clixml $agency.Name

    $importObj = CreateImportObject -ObjectType $ObjectType

    foreach ($attrib in $hashObj.Keys)
    {
        if ($attrib -eq "DisplayName")
        {
            [string]$NewDisplayName = "Copy of " + $HashObj.DisplayName
            SetSingleValue -ImportObject $importObj -AttributeName "DisplayName" -NewAttributeValue $NewDisplayName
        }
        elseif ($SkipAttribs -contains $attrib) {}
        elseif ($hashobj.$attrib.Count -gt 1)
        {
            foreach ($val in $hashobj.$attrib)
            {
            AddMultiValue -ImportObject $importObj -AttributeName $attrib -NewAttributeValue $val
            }
        }
        else
        {
            SetSingleValue -ImportObject $importObj -AttributeName $attrib -NewAttributeValue $hashobj.$attrib
        }
        }

        Import-FIMConfig -ImportObject $importObj
}
