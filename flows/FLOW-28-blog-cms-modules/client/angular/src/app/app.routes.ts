import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'blog-cms-modules/questionnaire',
    component: App,
  },
  {
    path: 'blog-cms-modules/matching',
    component: App,
  },
  {
    path: 'blog-cms-modules/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;




