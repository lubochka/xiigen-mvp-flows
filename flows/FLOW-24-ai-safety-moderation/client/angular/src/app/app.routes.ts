import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'ai-safety-moderation/questionnaire',
    component: App,
  },
  {
    path: 'ai-safety-moderation/matching',
    component: App,
  },
  {
    path: 'ai-safety-moderation/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

