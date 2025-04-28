export interface CancellationPolicy {
  id: number;
  name: string;
  description: string;
  refundPercentage: number;
}

export interface RefundCalculation {
  refundPercentage: number;
  refundAmount: number;
  isEligibleForRefund: boolean;
  policyName: string;
  policyDescription: string;
  daysUntilCheckIn: number;
} 