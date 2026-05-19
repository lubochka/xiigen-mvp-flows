import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'tenant_user' | 'referral_user' | 'freelancer' | 'event_organiser' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'completion' | 'dashboard' | 'social' | 'progress';

interface RewardRow {
  label: string;
  value: string;
  detail: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = ['tenant_user', 'referral_user', 'freelancer', 'event_organiser', 'tenant_admin'];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('completion');
  readonly moduleName = signal('Completion Gamification');
  readonly learnerName = signal('Maya Chen');
  readonly lessonName = signal('Launch readiness lesson');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'השלמת שיעור ותגמולים' : this.moduleName());
  readonly isRtl = computed(() => this.language() === 'he');
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      tenant_user: 'role-tenant-user-gamification',
      referral_user: 'role-referral-user-gamification',
      freelancer: 'role-freelancer-gamification',
      event_organiser: 'role-event-organiser-gamification',
      tenant_admin: 'role-tenant-admin-gamification'
    };
    return ids[this.role()];
  });

  readonly rewardRows: RewardRow[] = [
    { label: 'Completion recorded', value: '1', detail: 'Saved before reward messages are sent' },
    { label: 'Points awarded', value: '+210', detail: 'Calculated from the stored score' },
    { label: 'Current streak', value: '4 days', detail: 'Uses the learner local day' },
    { label: 'Achievements', value: '3', detail: 'Shared only with consent' }
  ];

  readonly progressRows = [
    { learner: 'Maya Chen', status: 'Completed', detail: 'High score badge unlocked' },
    { learner: 'Ari Levin', status: 'In review', detail: 'Peer grade pending' },
    { learner: 'Noa Bar', status: 'Shared', detail: 'Portfolio story created' },
    { learner: 'Samir Haddad', status: 'Next lesson', detail: 'Curriculum suggestion ready' }
  ];

  readonly checkpoints = [
    'Points come from the stored questionnaire score',
    'Streaks use the learner local day',
    'Sharing waits for an unlocked achievement and consent',
    'Repeated submits show the same reward result'
  ];

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
    const params = new URLSearchParams(this.document.defaultView?.location.search ?? '');
    const requestedRole = params.get('role');
    const requestedState = params.get('state');
    const requestedModule = params.get('module');
    const requestedLanguage: Language = params.get('lang') === 'he' ? 'he' : 'en';

    if (this.isViewerRole(requestedRole)) {
      this.role.set(requestedRole);
    }
    if (this.isScreenState(requestedState)) {
      this.screenState.set(requestedState);
    }
    if (requestedModule) {
      this.moduleName.set(this.toTitle(requestedModule));
    }
    this.applyLanguage(requestedLanguage);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      tenant_user: 'Learner',
      referral_user: 'Peer reviewer',
      freelancer: 'Portfolio owner',
      event_organiser: 'Learning organiser',
      tenant_admin: 'Tenant admin'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      tenant_user: 'Your completion reward',
      referral_user: 'Peer review and grading',
      freelancer: 'Portfolio-ready achievement',
      event_organiser: 'Learning cohort progress',
      tenant_admin: 'Tenant reward operations'
    };
    return headings[this.role()];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['completion', 'dashboard', 'social', 'progress'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
