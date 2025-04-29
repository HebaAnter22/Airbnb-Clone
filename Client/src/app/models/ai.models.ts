export interface AIRequestDTO {
  Query: string;
  RequestType: string;
}

export interface AIResponseDTO {
  success: boolean;
  response: string;
  error?: string;
} 