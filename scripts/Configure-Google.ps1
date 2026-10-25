[CmdletBinding(SupportsShouldProcess=$true, DefaultParameterSetName='File')]
param(
    [Parameter(Mandatory, ParameterSetName='File')][string]$ClientFile,
    [Parameter(Mandatory, ParameterSetName='Credential')][string]$ClientId,
    [Parameter(Mandatory, ParameterSetName='Credential')][securestring]$ClientSecret,
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$HostRepository='muhammetozeski/BinSite'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# The owner runs this once after creating a Google web OAuth client, and again only
# when replacing its credentials. The website never executes this configuration tool.
$secretPointer=[IntPtr]::Zero
$plainSecret=$null
try {
    if($PSCmdlet.ParameterSetName -eq 'File') {
        try { $configuration=[IO.File]::ReadAllText([IO.Path]::GetFullPath($ClientFile)) | ConvertFrom-Json }
        catch { throw 'Choose the downloaded Google web-client JSON file.' }
        $webProperty=$configuration.PSObject.Properties['web']
        if(!$webProperty){throw 'A web application client is required; a desktop client cannot use the production callback.'}
        $idProperty=$webProperty.Value.PSObject.Properties['client_id']
        $secretProperty=$webProperty.Value.PSObject.Properties['client_secret']
        if(!$idProperty -or !$secretProperty){throw 'The web-client file is missing its client ID or secret.'}
        $ClientId=[string]$idProperty.Value
        $plainSecret=[string]$secretProperty.Value
    } else {
        $secretPointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($ClientSecret)
        $plainSecret=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer)
    }
    $ClientId=$ClientId.Trim();$plainSecret=$plainSecret.Trim()
    if(!$ClientId.EndsWith('.apps.googleusercontent.com') -or $ClientId -match '\s' -or $ClientId.Length -gt 300 -or !$plainSecret -or $plainSecret.Length -gt 1000){throw 'The Google web-client configuration is invalid.'}
    if($PSCmdlet.ShouldProcess($HostRepository,'Save the Google web-client credential and publish Release Deck')) {
        # One encrypted repository secret keeps the credential pair together. It is
        # sent over standard input, never a process argument or workflow input.
        @{clientId=$ClientId;clientSecret=$plainSecret} | ConvertTo-Json -Compress | & gh secret set RELEASE_GOOGLE_WEB_CLIENT --app actions --repo $HostRepository
        if($LASTEXITCODE -ne 0){throw 'The private repository credential could not be saved; publication was not started.'}
        & gh workflow run deploy.yml --repo $HostRepository
        if($LASTEXITCODE -ne 0){throw 'The credential was saved, but publication could not start. Dispatch the existing deployment workflow.'}
        Write-Output 'Saved the web-client credential and started publication. After it succeeds, each creator connects their own channel from Delivery.'
    }
} finally {
    if($secretPointer -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer)}
    $plainSecret=$null
}
