import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_44_ROUTES: Routes = [
  {
    path: 'framer-adapter/filter',
    component: App,
  },
  {
    path: 'framer-adapter/style',
    component: App,
  },
  {
    path: 'framer-adapter/storage',
    component: App,
  },
];

export const routes: Routes = FLOW_44_ROUTES;
