# Testing Stripe Webhooks

This document explains how to test Stripe webhook events in development mode for the host payout system.

## Overview

The Stripe Connect implementation for host payouts uses webhooks to receive real-time updates about transfers and account status. For testing purposes, we've created tools to simulate these webhook events locally.

## Prerequisites

1. Your API server must be running locally (typically on `https://localhost:7228`)
2. The database should contain host records with Stripe account IDs
3. You should have created at least one payout request in the system

## Available Webhook Events

The following webhook events can be simulated:

1. `account.updated` - Triggered when a host's Stripe account status changes
2. `transfer.created` - Triggered when a transfer is initiated to the host's account
3. `transfer.paid` - Triggered when a transfer is successfully completed
4. `transfer.failed` - Triggered when a transfer fails

## Testing with PowerShell (Windows)

Open PowerShell and run:

```powershell
# Replace with actual payout ID and host ID from your database
.\TestStripeWebhook.ps1 -EventType "transfer.created" -PayoutId 1 -HostId 2
```

### Parameters:

- `EventType`: The type of event to simulate (one of the events listed above)
- `PayoutId`: The ID of a payout record in your database
- `HostId`: The ID of the host in your database

## Testing with Bash (Linux/Mac)

Open a terminal and run:

```bash
# Make the script executable first
chmod +x test-stripe-webhook.sh

# Replace with actual payout ID and host ID from your database
./test-stripe-webhook.sh transfer.created 1 2
```

### Parameters:

1. Event type (e.g., `transfer.created`)
2. Payout ID from your database
3. Host ID from your database

## Testing Flow

A typical testing flow would be:

1. Create a host account in the system
2. Set up a Stripe Connect account for the host
3. Request a payout from the host dashboard
4. Use the webhook testing tools to simulate the payout process:
   - First send a `transfer.created` event to update the payout status to "Processing"
   - Then send either a `transfer.paid` event (success) or a `transfer.failed` event (failure)
5. Verify the payout status is updated correctly in the database and UI

## Production vs. Development Mode

In development mode, the webhook controller allows testing without signature verification. In production, all webhooks must be properly signed with your webhook secret.

## Important Notes

- These tools are for testing purposes only and should not be used in production
- In a real production environment, you would register your webhook URL with Stripe and receive real events
- The webhook secret in your appsettings.json should be kept confidential in production environments 