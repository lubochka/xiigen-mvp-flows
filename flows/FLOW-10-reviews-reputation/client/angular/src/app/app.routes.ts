import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'reviews-reputation/questionnaire',
    component: App,
  },
  {
    path: 'reviews-reputation/matching',
    component: App,
  },
  {
    path: 'reviews-reputation/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
