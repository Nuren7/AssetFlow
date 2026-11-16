import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatRequest, ChatResponse } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  private readonly http = inject(HttpClient);

  sendMessage(message: string): Observable<ChatResponse> {
    const request: ChatRequest = { message };
    return this.http.post<ChatResponse>(`${environment.apiUrl}/assistant/chat`, request);
  }
}
