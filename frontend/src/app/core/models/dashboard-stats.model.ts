import { AssetCategory, AssetStatus } from './asset.model';

export interface DashboardStats {
  totalAssets: number;
  availableCount: number;
  assignedCount: number;
  inRepairCount: number;
  expiringWithin30DaysCount: number;
  countByCategory: Partial<Record<AssetCategory, number>>;
  countByStatus: Partial<Record<AssetStatus, number>>;
}
