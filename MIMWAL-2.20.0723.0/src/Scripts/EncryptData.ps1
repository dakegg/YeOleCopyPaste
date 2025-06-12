<#
    This script demonstrates how the credentials can be encrypted for the RunAs user in the RunPowerShellScript Activity.

    If the password is encrypted using certificate based encryption (recommended due to ease of deployment), please make sure that you deploy that certificate on the the FIMService nodes. It can be an auto-genenrated cert.
    If the password is encrypted using DPAPI, ensure that you export and import the RSA machine keys. Follow the steps documented for web farm scenario mentioned in the article
    at http://msdn.microsoft.com/en-us/library/ms998283.aspx (How To: Encrypt Configuration Sections in ASP.NET 2.0 Using RSA), but for the FIM Service account.

	NOTE: Edit the Version and PublicKeyToken of the WAL AssemblyName to match the one that you have deployed in GAC.
	Also edit the $encryptionCertThumbprint of cert to be used for certificate based encryption.
	
    To find Assembly verion and PublicKeyToken
        .\gacutil.exe -l | findstr WorkflowActivityLibrary
    To create a self-signed certificate for MIMWAL, you must use a legacy CSP (as .NET 3.5 only supports legacy CSPs).
    You can use a legacy CSP such as Microsoft Strong Cryptographic Provider as shown in the example below:
        $cert = New-SelfSignedCertificate -DnsName "MIMWAL Encryption (Do Not Delete)" -CertStoreLocation "cert:\LocalMachine\My" -Provider "Microsoft Strong Cryptographic Provider" -NotAfter (Get-Date).AddYears(20)
        $cert.Thumbprint
        As of version v2.18.1110.0, only FIMService account needs read access to the private key of the MIMWAL certificate created above.
#>

$Error.Clear()

$walAssemblyVersion = "2.20.0723.0" # edit appropriately
$walAssemblyPublicKeyToken = "31bf3856ad364e35" # edit appropriately
$encryptionCertThumbprint = "9C697919FB2FB2D6324ADE42D5F8CB49E8778C08" # cert to be used for encryption (from the cert:\localmachine\my\ store). Edit appropriately

Add-Type -AssemblyName "System.Security"
# use the full name for WAL assembly to eliminate need to assembly redirects for dependent assemblies.
Add-Type -AssemblyName "MicrosoftServices.IdentityManagement.WorkflowActivityLibrary, Version=$walAssemblyVersion, Culture=neutral, PublicKeyToken=$walAssemblyPublicKeyToken"

if ($Error)
{
    Write-Host "Aborting script execution."
    return
}

function TestCertificateBasedEncryptionDecryption
{
	$outFile = Join-Path -Path $PWD -ChildPath "cert-p.txt"

    $base64EncodedPublicKeyXml = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::GetCertificatePublicKeyXml($encryptionCertThumbprint, "My", "LocalMachine")

    $secretToEncrypt = "Pass@word1"

    $secret = ConvertTo-SecureString -AsPlainText $secretToEncrypt -Force

    $encryptedData = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::EncryptData($secret, $base64EncodedPublicKeyXml)

    $encryptedDataConfig = "cert:\localmachine\my\$encryptionCertThumbprint,$encryptedData"

    $encryptedDataConfig | Out-File $outFile

    $decryptedData = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::DecryptData($encryptedDataConfig)

    $plainText = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::ConvertToUnsecureString($decryptedData)

    if ($plainText -eq $secretToEncrypt)
    {
        Write-Host "`nEncryption and Decryption test using certificate '$encryptionCertThumbprint' succeeded!`n"
        Write-Host "`nThe password config is saved in '$outFile'`n"
    }
    else
    {
        Write-Error "`nEncryption and Decryption test using certificate '$encryptionCertThumbprint' failed!`n"
    }
}

function TestDPAPIBasedEncryptionDecryption
{
    $secretToEncrypt = "Pass@word1"
	$outFile = Join-Path -Path $PWD -ChildPath "dpapi-p.txt"

    $secret = ConvertTo-SecureString -AsPlainText $secretToEncrypt -Force

    $encryptedData = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::EncryptData($secret, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)

    $encryptedData | Out-File $outFile

    $decryptedData = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::DecryptData($encryptedData, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)

    $plainText = [MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common.ProtectedData]::ConvertToUnsecureString($decryptedData)

    if ($plainText -eq $secretToEncrypt)
    {
        Write-Host "`nEncryption and Decryption test using DPAPI succeeded!`n"
        Write-Host "`nThe encrypted password is saved in '$outFile'`n"
    }
    else
    {
        Write-Error "`nEncryption and Decryption test using DPAPI failed!`n"
    }
}

TestCertificateBasedEncryptionDecryption
#TestDPAPIBasedEncryptionDecryption

