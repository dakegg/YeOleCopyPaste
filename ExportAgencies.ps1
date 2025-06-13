function ConvertResourceToHashtable
{
    PARAM([Microsoft.ResourceManagement.Automation.ObjectModel.ExportObject]$ExportObject)
    END
    {
        $hashtable = @{"ObjectID" = "Not found"}
        foreach($attribute in $exportObject.ResourceManagementObject.ResourceManagementAttributes)
        {
            if ($attribute.IsMultiValue -eq 1)
            {
                $hashtable[$attribute.AttributeName] = $attribute.Values
            }
            else
            {
                $hashtable[$attribute.AttributeName] = $attribute.Value
            }
        }
        $hashtable
    }
}

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


[string]$filter = "/Agency"
$Agencies = export-fimconfig -customConfig $filter -OnlyBaseResources

Foreach ($Agency in $Agencies)
{
    $hashObj = $null
    $DisplayName = $null
    $hashObj = ConvertResourceToHashtable $Agency
    $DisplayName = $hashObj.DisplayName
    
    if ($DisplayName ) {$hashObj | Export-CliXML "$DisplayName.xml"}
    
}