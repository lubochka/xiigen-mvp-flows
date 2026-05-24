import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_29_ROUTES: Routes = [
  {
    path: 'tenant-lifecycle-manager/routing',
    component: App,
  },
  {
    path: 'tenant-lifecycle-manager/graph',
    component: App,
  },
  {
    path: 'tenant-lifecycle-manager/promotion',
    component: App,
  },
];

export const routes: Routes = FLOW_29_ROUTES;




