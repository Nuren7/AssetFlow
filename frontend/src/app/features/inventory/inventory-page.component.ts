import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { ApiProblemError } from '../../core/interceptors/api-error.interceptor';
import { AssetApiService } from '../../core/services/asset-api.service';
import { Asset, AssetCategory, AssetStatus, ASSET_CATEGORIES, ASSET_STATUSES } from '../../core/models/asset.model';
import { DashboardStats } from '../../core/models/dashboard-stats.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

const warrantyDateValidator: ValidatorFn = control => {
  const purchaseDate = control.get('purchaseDate')?.value as string | undefined;
  const warrantyDate = control.get('warrantyExpirationDate')?.value as string | undefined;
  return purchaseDate && warrantyDate && warrantyDate < purchaseDate
    ? { warrantyBeforePurchase: true }
    : null;
};

const purchaseDateValidator: ValidatorFn = control => {
  const purchaseDate = control.value as string | undefined;
  const today = new Date().toISOString().slice(0, 10);
  return purchaseDate && purchaseDate > today ? { futurePurchaseDate: true } : null;
};

@Component({
  selector: 'app-inventory-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StatusBadgeComponent, ConfirmDialogComponent],
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InventoryPageComponent implements OnInit {
  private readonly assetApi = inject(AssetApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly allAssets = signal<Asset[]>([]);

  readonly assets = signal<Asset[]>([]);
  readonly stats = signal<DashboardStats | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly editorOpen = signal(false);
  readonly editingAsset = signal<Asset | null>(null);
  readonly deletingAsset = signal<Asset | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly fieldErrors = signal<Readonly<Record<string, string[]>>>({});
  readonly categories = ASSET_CATEGORIES;
  readonly statuses = ASSET_STATUSES;
  readonly currentDate = new Intl.DateTimeFormat('en', { weekday: 'short', month: 'short', day: 'numeric' }).format(new Date());
  readonly warrantyAlerts = computed(() => {
    const today = this.today();
    return this.allAssets()
      .filter(asset => {
        const days = this.daysUntil(asset.warrantyExpirationDate, today);
        return days >= 0 && days <= 30;
      })
      .sort((left, right) => left.warrantyExpirationDate.localeCompare(right.warrantyExpirationDate))
      .slice(0, 3);
  });

  readonly filtersForm = this.formBuilder.nonNullable.group({
    search: '',
    category: '' as AssetCategory | '',
    status: '' as AssetStatus | ''
  });

  readonly assetForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    serialNumber: ['', [Validators.required, Validators.maxLength(120)]],
    category: ['Laptop' as AssetCategory, Validators.required],
    manufacturer: ['', [Validators.required, Validators.maxLength(120)]],
    model: ['', [Validators.required, Validators.maxLength(120)]],
    status: ['Available' as AssetStatus, Validators.required],
    location: ['', [Validators.required, Validators.maxLength(200)]],
    purchaseDate: ['', [Validators.required, purchaseDateValidator]],
    warrantyExpirationDate: ['', Validators.required]
  }, { validators: warrantyDateValidator });

  ngOnInit(): void {
    this.loadInventory();
    this.filtersForm.valueChanges
      .pipe(
        startWith(this.filtersForm.getRawValue()),
        debounceTime(250),
        distinctUntilChanged((previous, current) =>
          previous.search === current.search &&
          previous.category === current.category &&
          previous.status === current.status),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(filters => {
        this.assetApi.getAssets(filters).subscribe({
          next: assets => this.assets.set(assets),
          error: error => this.showError(error)
        });
      });
  }

  loadInventory(): void {
    this.loading.set(true);
    this.assetApi.getAssets().subscribe({
      next: assets => {
        this.allAssets.set(assets);
        this.assets.set(assets);
        this.loading.set(false);
      },
      error: error => {
        this.showError(error);
        this.loading.set(false);
      }
    });
    this.assetApi.getStats().subscribe({
      next: stats => this.stats.set(stats),
      error: error => this.showError(error)
    });
  }

  openCreate(): void {
    this.editingAsset.set(null);
    this.fieldErrors.set({});
    this.errorMessage.set('');
    this.assetForm.reset({
      name: '',
      serialNumber: '',
      category: 'Laptop',
      manufacturer: '',
      model: '',
      status: 'Available',
      location: '',
      purchaseDate: '',
      warrantyExpirationDate: ''
    });
    this.editorOpen.set(true);
  }

  openEdit(asset: Asset): void {
    this.editingAsset.set(asset);
    this.fieldErrors.set({});
    this.errorMessage.set('');
    this.assetForm.reset({
      name: asset.name,
      serialNumber: asset.serialNumber,
      category: asset.category,
      manufacturer: asset.manufacturer,
      model: asset.model,
      status: asset.status,
      location: asset.location,
      purchaseDate: asset.purchaseDate,
      warrantyExpirationDate: asset.warrantyExpirationDate
    });
    this.editorOpen.set(true);
  }

  closeEditor(): void {
    if (!this.saving()) {
      this.editorOpen.set(false);
    }
  }

  saveAsset(): void {
    this.assetForm.markAllAsTouched();
    if (this.assetForm.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.fieldErrors.set({});
    this.errorMessage.set('');
    const request = this.assetForm.getRawValue();
    const asset = this.editingAsset();
    const operation = asset
      ? this.assetApi.updateAsset(asset.id, request)
      : this.assetApi.createAsset(request);

    operation.subscribe({
      next: () => {
        this.editorOpen.set(false);
        this.saving.set(false);
        this.successMessage.set(asset ? 'Asset details updated.' : 'Asset added to inventory.');
        this.loadInventory();
      },
      error: error => {
        this.applyFormError(error);
        this.saving.set(false);
      }
    });
  }

  confirmDelete(): void {
    const asset = this.deletingAsset();
    if (!asset) {
      return;
    }

    this.assetApi.deleteAsset(asset.id).subscribe({
      next: () => {
        this.deletingAsset.set(null);
        this.successMessage.set(`${asset.name} was removed from inventory.`);
        this.loadInventory();
      },
      error: error => {
        this.deletingAsset.set(null);
        this.showError(error);
      }
    });
  }

  clearFilters(): void {
    this.filtersForm.reset({ search: '', category: '', status: '' });
  }

  isWarrantySoon(asset: Asset): boolean {
    const days = this.daysUntil(asset.warrantyExpirationDate, this.today());
    return days >= 0 && days <= 30;
  }

  warrantyLabel(asset: Asset): string {
    const days = this.daysUntil(asset.warrantyExpirationDate, this.today());
    if (days === 0) {
      return 'Expires today';
    }
    if (days === 1) {
      return '1 day left';
    }
    return `${days} days left`;
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' })
      .format(new Date(`${date}T00:00:00`));
  }

  fieldError(field: string): string {
    const backendError = this.backendFieldErrors(field)?.[0];
    if (backendError) {
      return backendError;
    }
    const control = this.assetForm.get(field);
    if (control?.hasError('required')) {
      return 'This field is required.';
    }
    if (control?.hasError('maxlength')) {
      return `Use ${control.getError('maxlength').requiredLength} characters or fewer.`;
    }
    if (control?.hasError('futurePurchaseDate')) {
      return 'Purchase date cannot be in the future.';
    }
    if (field === 'warrantyExpirationDate' && this.assetForm.hasError('warrantyBeforePurchase')) {
      return 'Warranty expiration cannot be before purchase.';
    }
    return '';
  }

  isFieldInvalid(field: string): boolean {
    const control = this.assetForm.get(field);
    return Boolean(control && control.invalid && (control.touched || control.dirty)) ||
      Boolean(this.backendFieldErrors(field)?.length);
  }

  dismissMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private applyFormError(error: unknown): void {
    if (error instanceof ApiProblemError) {
      this.fieldErrors.set(error.fieldErrors);
      this.errorMessage.set(error.message);
      return;
    }
    this.showError(error);
  }

  private showError(error: unknown): void {
    this.errorMessage.set(
      error instanceof Error ? error.message : 'Something went wrong. Please try again.'
    );
  }

  private backendFieldErrors(field: string): string[] | undefined {
    const entry = Object.entries(this.fieldErrors())
      .find(([key]) => key.toLowerCase() === field.toLowerCase());
    return entry?.[1];
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private daysUntil(date: string, today: string): number {
    return Math.round((Date.parse(`${date}T00:00:00Z`) - Date.parse(`${today}T00:00:00Z`)) / 86_400_000);
  }
}
