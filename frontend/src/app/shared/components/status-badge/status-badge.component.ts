import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AssetStatus } from '../../../core/models/asset.model';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="'status-badge ' + statusClass()">
      <span class="badge-dot"></span>
      {{ statusLabel() }}
    </span>
  `,
  styleUrl: './status-badge.component.scss'
})
export class StatusBadgeComponent {
  readonly status = input.required<AssetStatus>();

  statusClass(): string {
    return `status-badge--${this.status().toLowerCase()}`;
  }

  statusLabel(): string {
    return this.status() === 'InRepair' ? 'In repair' : this.status();
  }
}
