export const ASSET_CATEGORIES = ['Laptop', 'Monitor', 'Mobile', 'Accessory', 'Network', 'Other'] as const;
export type AssetCategory = typeof ASSET_CATEGORIES[number];

export const ASSET_STATUSES = ['Available', 'Assigned', 'InRepair', 'Retired'] as const;
export type AssetStatus = typeof ASSET_STATUSES[number];

export interface Asset {
  id: string;
  name: string;
  serialNumber: string;
  category: AssetCategory;
  manufacturer: string;
  model: string;
  status: AssetStatus;
  location: string;
  purchaseDate: string;
  warrantyExpirationDate: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface AssetRequest {
  name: string;
  serialNumber: string;
  category: AssetCategory;
  manufacturer: string;
  model: string;
  status: AssetStatus;
  location: string;
  purchaseDate: string;
  warrantyExpirationDate: string;
}

export interface AssetFilters {
  search?: string;
  category?: AssetCategory | '';
  status?: AssetStatus | '';
}
