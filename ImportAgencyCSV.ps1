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

$agenciesCSV = Import-Csv .\AgenciesCSV.csv -Header "AgencyOUPath","AgencyAcronym"

Foreach ($Record in $AgenciesCSV)
{

    $importObj = $null
    $importObj = CreateImportObject -ObjectType "Agency"

    SetSingleValue -ImportObject $importObj -AttributeName "DisplayName" -NewAttributeValue $Record.AgencyAcronym
    SetSingleValue -ImportObject $importObj -AttributeName "AgencyAcronym" -NewAttributeValue $Record.AgencyAcronym
    SetSingleValue -ImportObject $importObj -AttributeName "AgencyOUPath" -NewAttributeValue $Record.AgencyOUPath

    Import-FIMConfig -ImportObject $importObj
}