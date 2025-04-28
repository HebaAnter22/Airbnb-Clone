#!/bin/bash
# Bash script to test Stripe webhooks locally
# This script simulates Stripe webhook events for testing purposes

# Check for required arguments
if [ $# -lt 3 ]; then
    echo "Usage: $0 <event_type> <payout_id> <host_id>"
    echo "Event types: account.updated, transfer.created, transfer.paid, transfer.failed"
    exit 1
fi

EVENT_TYPE=$1
PAYOUT_ID=$2
HOST_ID=$3
API_URL="https://localhost:7228/api/StripeWebhook"

# Generate a random UUID
generate_uuid() {
    cat /dev/urandom | tr -dc 'a-f0-9' | fold -w 32 | head -n 1
}

# Get current timestamp
TIMESTAMP=$(date +%s)

# Create different payload types based on the event type
case $EVENT_TYPE in
    "account.updated")
        ACCT_ID="acct_$(generate_uuid | cut -c1-16)"
        PAYLOAD=$(cat <<EOF
{
    "id": "evt_$(generate_uuid)",
    "object": "event",
    "api_version": "2020-08-27",
    "created": $TIMESTAMP,
    "type": "account.updated",
    "data": {
        "object": {
            "id": "$ACCT_ID",
            "object": "account",
            "charges_enabled": true,
            "details_submitted": true,
            "payouts_enabled": true,
            "metadata": {
                "HostId": "$HOST_ID"
            }
        }
    }
}
EOF
)
        ;;
    "transfer.created")
        TRANSFER_ID="tr_$(generate_uuid | cut -c1-16)"
        ACCT_ID="acct_$(generate_uuid | cut -c1-16)"
        PAYLOAD=$(cat <<EOF
{
    "id": "evt_$(generate_uuid)",
    "object": "event",
    "api_version": "2020-08-27",
    "created": $TIMESTAMP,
    "type": "transfer.created",
    "data": {
        "object": {
            "id": "$TRANSFER_ID",
            "object": "transfer",
            "amount": 1000,
            "currency": "usd",
            "destination": "$ACCT_ID",
            "metadata": {
                "HostId": "$HOST_ID",
                "PayoutId": "$PAYOUT_ID"
            }
        }
    }
}
EOF
)
        ;;
    "transfer.paid")
        TRANSFER_ID="tr_$(generate_uuid | cut -c1-16)"
        ACCT_ID="acct_$(generate_uuid | cut -c1-16)"
        PAYLOAD=$(cat <<EOF
{
    "id": "evt_$(generate_uuid)",
    "object": "event",
    "api_version": "2020-08-27",
    "created": $TIMESTAMP,
    "type": "transfer.paid",
    "data": {
        "object": {
            "id": "$TRANSFER_ID",
            "object": "transfer",
            "amount": 1000,
            "currency": "usd",
            "destination": "$ACCT_ID",
            "metadata": {
                "HostId": "$HOST_ID",
                "PayoutId": "$PAYOUT_ID"
            }
        }
    }
}
EOF
)
        ;;
    "transfer.failed")
        TRANSFER_ID="tr_$(generate_uuid | cut -c1-16)"
        ACCT_ID="acct_$(generate_uuid | cut -c1-16)"
        PAYLOAD=$(cat <<EOF
{
    "id": "evt_$(generate_uuid)",
    "object": "event",
    "api_version": "2020-08-27",
    "created": $TIMESTAMP,
    "type": "transfer.failed",
    "data": {
        "object": {
            "id": "$TRANSFER_ID",
            "object": "transfer",
            "amount": 1000,
            "currency": "usd",
            "destination": "$ACCT_ID",
            "metadata": {
                "HostId": "$HOST_ID",
                "PayoutId": "$PAYOUT_ID"
            }
        }
    }
}
EOF
)
        ;;
    *)
        echo "Unsupported event type: $EVENT_TYPE"
        exit 1
        ;;
esac

echo "Sending $EVENT_TYPE webhook event to $API_URL"
echo "Payload: $PAYLOAD"

# Send POST request to the webhook endpoint
# Note: For production, you'd need to handle TLS/SSL certificate validation properly
# Using curl with -k to skip certificate validation for local testing
curl -k -X POST "$API_URL" \
     -H "Content-Type: application/json" \
     -d "$PAYLOAD"

echo 
echo "Webhook test completed!" 