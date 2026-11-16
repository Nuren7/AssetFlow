import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiProblemError } from '../../core/interceptors/api-error.interceptor';
import { ChatTurn } from '../../core/models/chat.model';
import { AssistantApiService } from '../../core/services/assistant-api.service';

@Component({
  selector: 'app-assistant-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assistant-page.component.html',
  styleUrl: './assistant-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssistantPageComponent {
  private readonly assistantApi = inject(AssistantApiService);
  private readonly thread = viewChild<ElementRef<HTMLDivElement>>('thread');

  readonly question = signal('');
  readonly turns = signal<ChatTurn[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly suggestions = [
    'How many laptops are currently available?',
    'Which assets have warranties expiring within 30 days?',
    'Show me all Dell devices',
    'How many devices are currently in repair?'
  ];

  constructor() {
    effect(() => {
      this.turns();
      queueMicrotask(() => {
        const element = this.thread()?.nativeElement;
        if (element) {
          element.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
        }
      });
    });
  }

  useSuggestion(suggestion: string): void {
    this.question.set(suggestion);
  }

  sendMessage(): void {
    const message = this.question().trim();
    if (!message || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.question.set('');

    this.assistantApi.sendMessage(message).subscribe({
      next: response => {
        this.turns.update(turns => [...turns, {
          question: message,
          answer: response.answer,
          toolsUsed: response.toolsUsed ?? [],
          askedAt: new Date()
        }]);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(error instanceof ApiProblemError
          ? error.message
          : 'The assistant could not complete that request. Please try again.');
        this.loading.set(false);
        this.question.set(message);
      }
    });
  }

  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
