param (
    [Parameter(Mandatory=$true)]
    [string]$EventType,
    
    [Parameter(Mandatory=$true)]
    [string]$PayoutId,
    
    [Parameter(Mandatory=$true)]
    [string]$HostId
)



# Set API endpoint
$apiUrl = "https://localhost:7228/api/StripeWebhook"

# Create different payload types based on the event type
switch ($EventType) {
    "account.updated" {
        $payload = @{
            id = "evt_" + (New-Guid).ToString("N")
            object = "event"
            api_version = "2020-08-27"
            created = [int](Get-Date -UFormat %s)
            type = "account.updated"
            data = @{
                object = @{
                    id = "acct_" + (New-Guid).ToString("N").Substring(0, 16)
                    object = "account"
                    charges_enabled = $true
                    details_submitted = $true
                    payouts_enabled = $true
                    metadata = @{
                        HostId = $HostId
                    }
                }
            }
        }
    }
    "transfer.created" {
        $payload = @{
            id = "evt_" + (New-Guid).ToString("N")
            object = "event"
            api_version = "2020-08-27"
            created = [int](Get-Date -UFormat %s)
            type = "transfer.created"
            data = @{
                object = @{
                    id = "tr_" + (New-Guid).ToString("N").Substring(0, 16)
                    object = "transfer"
                    amount = 1000
                    currency = "usd"
                    destination = "acct_" + (New-Guid).ToString("N").Substring(0, 16)
                    metadata = @{
                        HostId = $HostId
                        PayoutId = $PayoutId
                    }
                }
            }
        }
    }
    "transfer.paid" {
        $payload = @{
            id = "evt_" + (New-Guid).ToString("N")
            object = "event"
            api_version = "2020-08-27"
            created = [int](Get-Date -UFormat %s)
            type = "transfer.paid"
            data = @{
                object = @{
                    id = "tr_" + (New-Guid).ToString("N").Substring(0, 16)
                    object = "transfer"
                    amount = 1000
                    currency = "usd"
                    destination = "acct_" + (New-Guid).ToString("N").Substring(0, 16)
                    metadata = @{
                        HostId = $HostId
                        PayoutId = $PayoutId
                    }
                }
            }
        }
    }
    "transfer.failed" {
        $payload = @{
            id = "evt_" + (New-Guid).ToString("N")
            object = "event"
            api_version = "2020-08-27"
            created = [int](Get-Date -UFormat %s)
            type = "transfer.failed"
            data = @{
                object = @{
                    id = "tr_" + (New-Guid).ToString("N").Substring(0, 16)
                    object = "transfer"
                    amount = 1000
                    currency = "usd"
                    destination = "acct_" + (New-Guid).ToString("N").Substring(0, 16)
                    metadata = @{
                        HostId = $HostId
                        PayoutId = $PayoutId
                    }
                }
            }
        }
    }
    default {
        Write-Error "Unsupported event type: $EventType"
        exit 1
    }
}

# Convert payload to JSON
$jsonPayload = $payload | ConvertTo-Json -Depth 10

# Note: In a real scenario, you would need to sign the payload with your webhook secret
# For testing, we're bypassing that requirement by adding the signature verification in the controller

Write-Host "Sending $EventType webhook event to $apiUrl"
Write-Host "Payload: $jsonPayload"

try {
    # Send POST request to the webhook endpoint
    # Note: For production, you'd need to handle TLS/SSL certificate validation properly
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    
    $response = Invoke-WebRequest -Uri $apiUrl -Method Post -Body $jsonPayload -ContentType "application/json" -UseBasicParsing
    
    Write-Host "Response Status: $($response.StatusCode)"
    Write-Host "Response Body: $($response.Content)"
} catch {
    Write-Error "Error sending webhook: $_"
    exit 1
}

Write-Host "Webhook test completed successfully!" 