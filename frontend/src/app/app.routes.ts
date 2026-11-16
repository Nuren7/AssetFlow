import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'inventory',
    loadComponent: () => import('./features/inventory/inventory-page.component').then(module => module.InventoryPageComponent)
  },
  {
    path: 'assistant',
    loadComponent: () => import('./features/assistant/assistant-page.component').then(module => module.AssistantPageComponent)
  },
  { path: '', pathMatch: 'full', redirectTo: 'inventory' },
  { path: '**', redirectTo: 'inventory' }
];
