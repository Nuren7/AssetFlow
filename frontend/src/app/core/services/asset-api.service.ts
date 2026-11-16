import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Asset, AssetFilters, AssetRequest } from '../models/asset.model';
import { DashboardStats } from '../models/dashboard-stats.model';

@Injectable({ providedIn: 'root' })
export class AssetApiService {
  private readonly http = inject(HttpClient);
  private readonly assetsUrl = `${environment.apiUrl}/assets`;

  getAssets(filters: AssetFilters = {}): Observable<Asset[]> {
    let params = new HttpParams();
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }
    if (filters.category) {
      params = params.set('category', filters.category);
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    return this.http.get<Asset[]>(this.assetsUrl, { params });
  }

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.assetsUrl}/stats`);
  }

  createAsset(request: AssetRequest): Observable<Asset> {
    return this.http.post<Asset>(this.assetsUrl, request);
  }

  updateAsset(id: string, request: AssetRequest): Observable<Asset> {
    return this.http.put<Asset>(`${this.assetsUrl}/${id}`, request);
  }

  deleteAsset(id: string): Observable<void> {
    return this.http.delete<void>(`${this.assetsUrl}/${id}`);
  }
}
