import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_44_ROUTES: Routes = [
  {
    path: 'platform-agent/filter',
    component: App,
  },
  {
    path: 'platform-agent/style',
    component: App,
  },
  {
    path: 'platform-agent/storage',
    component: App,
  },
];

export const routes: Routes = FLOW_44_ROUTES;

