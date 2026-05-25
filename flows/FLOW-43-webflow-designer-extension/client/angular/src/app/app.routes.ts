import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_29_ROUTES: Routes = [
  {
    path: 'webflow-designer-extension/routing',
    component: App,
  },
  {
    path: 'webflow-designer-extension/graph',
    component: App,
  },
  {
    path: 'webflow-designer-extension/promotion',
    component: App,
  },
];

export const routes: Routes = FLOW_29_ROUTES;
