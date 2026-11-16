import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dialog-backdrop" (click)="cancel.emit()">
      <section
        class="confirm-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-title"
        (click)="$event.stopPropagation()">
        <div class="dialog-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M4 7h16M9 7V4h6v3m3 0-.8 13H6.8L6 7m4 4v5m4-5v5" />
          </svg>
        </div>
        <h2 id="confirm-title">{{ title() }}</h2>
        <p>{{ message() }}</p>
        <div class="dialog-actions">
          <button class="button button--quiet" type="button" (click)="cancel.emit()">Cancel</button>
          <button class="button button--danger" type="button" (click)="confirm.emit()">Delete asset</button>
        </div>
      </section>
    </div>
  `,
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  readonly title = input('Delete asset?');
  readonly message = input('This asset will be permanently removed from your inventory.');
  readonly cancel = output<void>();
  readonly confirm = output<void>();
}
