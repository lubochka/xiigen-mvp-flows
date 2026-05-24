import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'cms-publishing/questionnaire',
    component: App,
  },
  {
    path: 'cms-publishing/matching',
    component: App,
  },
  {
    path: 'cms-publishing/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

