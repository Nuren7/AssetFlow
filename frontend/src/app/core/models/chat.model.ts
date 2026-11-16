export interface ChatRequest {
  message: string;
}

export interface ToolLog {
  toolName: string;
  arguments: string;
  resultSummary: string;
}

export interface ChatResponse {
  answer: string;
  toolsUsed: ToolLog[];
}

export interface ChatTurn extends ChatResponse {
  question: string;
  askedAt: Date;
}
