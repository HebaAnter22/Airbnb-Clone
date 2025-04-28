/**
 * This is a test script to manually test the booking cancellation functionality.
 * 
 * Instructions:
 * 1. Open console in browser
 * 2. Copy and paste this script
 * 3. Call the functions as needed
 */

// Test booking cancellation
async function testCancelBooking(bookingId: number) {
  const apiUrl = getApiUrl();
  try {
    // First get booking details
    const bookingDetails = await fetchBookingDetails(bookingId);
    console.log('Booking details:', bookingDetails);
    
    // Calculate refund based on cancellation policy
    const refundInfo = calculateRefund(bookingDetails);
    console.log('Refund calculation:', refundInfo);
    
    // Confirm cancellation
    if (confirm(`Are you sure you want to cancel this booking? ${refundInfo.isEligibleForRefund ? 
      `You will receive a refund of $${refundInfo.refundAmount.toFixed(2)}` : 
      'You will not receive a refund'}`)) {
      
      // Call the API to cancel the booking
      const response = await fetch(`${apiUrl}/Booking/${bookingId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${getToken()}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (response.ok) {
        console.log('Booking cancelled successfully');
        if (refundInfo.isEligibleForRefund) {
          console.log(`Refund of $${refundInfo.refundAmount.toFixed(2)} will be processed`);
        }
      } else {
        console.error('Failed to cancel booking:', await response.text());
      }
    }
  } catch (error) {
    console.error('Error:', error);
  }
}

// Helper functions
async function fetchBookingDetails(bookingId: number) {
  const apiUrl = getApiUrl();
  const response = await fetch(`${apiUrl}/Booking/${bookingId}/details`, {
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Failed to fetch booking details');
  }
  
  return await response.json();
}

function calculateRefund(booking: any) {
  const policy = booking.property?.cancellationPolicy;
  const startDate = new Date(booking.startDate);
  const now = new Date();
  const daysUntilCheckIn = Math.ceil((startDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  
  let isEligibleForRefund = false;
  let refundPercentage = 0;

  if (policy) {
    switch (policy.name.toLowerCase()) {
      case 'flexible':
        isEligibleForRefund = daysUntilCheckIn >= 1;
        refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
        break;
      case 'moderate':
        isEligibleForRefund = daysUntilCheckIn >= 5;
        refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
        break;
      case 'strict':
        isEligibleForRefund = daysUntilCheckIn >= 7;
        refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
        break;
      default:
        isEligibleForRefund = false;
        refundPercentage = 0;
    }
  }

  const refundAmount = booking.totalAmount * (refundPercentage / 100);

  return {
    refundPercentage,
    refundAmount,
    isEligibleForRefund,
    policyName: policy?.name || 'Unknown',
    policyDescription: policy?.description || 'No description available',
    daysUntilCheckIn
  };
}

function getApiUrl() {
  // This should match your environment.apiUrl value
  return 'http://localhost:5178/api';
}

function getToken() {
  // Get token from localStorage or sessionStorage
  return localStorage.getItem('auth_token') || '';
}

// Example usage:
// testCancelBooking(123); 