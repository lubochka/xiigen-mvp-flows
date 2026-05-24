import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'form-builder-templates/questionnaire',
    component: App,
  },
  {
    path: 'form-builder-templates/matching',
    component: App,
  },
  {
    path: 'form-builder-templates/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
