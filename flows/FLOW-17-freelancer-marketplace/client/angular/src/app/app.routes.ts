import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'freelancer-marketplace/questionnaire',
    component: App,
  },
  {
    path: 'freelancer-marketplace/matching',
    component: App,
  },
  {
    path: 'freelancer-marketplace/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
