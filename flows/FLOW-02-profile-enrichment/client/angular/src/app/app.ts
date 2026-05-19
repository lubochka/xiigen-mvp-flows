import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'tenant_admin'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState =
  | 'questionnaire'
  | 'processing'
  | 'validation'
  | 'matching-in-progress'
  | 'matching-partial'
  | 'matching-complete'
  | 'personalization-complete'
  | 'personalization-degraded';

interface MatchItem {
  id: string;
  name: string;
  score: number;
}

interface FeedItem {
  id: string;
  title: string;
  signal: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = [
    'anonymous',
    'tenant_user',
    'tenant_admin',
    'freelancer',
    'business_partner',
    'event_organiser',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('questionnaire');
  readonly moduleName = signal('Profile Enrichment');
  readonly industry = signal('Technology');
  readonly businessStage = signal('Startup');
  readonly partnerNeed = signal('Software development');
  readonly validationMessage = signal<string | null>(null);

  readonly isRtl = computed(() => this.language() === 'he');
  readonly isProtected = computed(() => this.role() === 'anonymous');
  readonly title = computed(() => this.language() === 'he' ? 'העשרת פרופיל והתאמה' : this.moduleName());

  readonly matches: MatchItem[] = [
    { id: 'match-1', name: 'Acme Ventures', score: 94 },
    { id: 'match-2', name: 'Beta Studio', score: 87 },
    { id: 'match-3', name: 'Gamma Labs', score: 81 }
  ];
  readonly partialMatches = this.matches.slice(0, 2);

  readonly feed: FeedItem[] = [
    { id: 'feed-1', title: 'Integration checklist for your industry', signal: 'profile' },
    { id: 'feed-2', title: 'Connections for your current growth stage', signal: 'matching' },
    { id: 'feed-3', title: 'Learning path for first workflow launch', signal: 'curriculum' },
    { id: 'feed-4', title: 'Peer examples from similar teams', signal: 'segment' }
  ];
  readonly fallbackFeed: FeedItem[] = [
    { id: 'fallback-1', title: 'Popular setup guide', signal: 'trending' },
    { id: 'fallback-2', title: 'Top community discussion', signal: 'trending' },
    { id: 'fallback-3', title: 'Common first automation', signal: 'trending' }
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
    this.validationMessage.set(null);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
    this.validationMessage.set(null);
  }

  updateIndustry(value: string): void {
    this.industry.set(value);
  }

  updateBusinessStage(value: string): void {
    this.businessStage.set(value);
  }

  updatePartnerNeed(value: string): void {
    this.partnerNeed.set(value);
  }

  submitQuestionnaire(): void {
    if (!this.industry().trim()) {
      this.screenState.set('validation');
      this.validationMessage.set('Please review your submission.');
      return;
    }

    this.validationMessage.set(null);
    this.screenState.set('processing');
  }

  skipQuestionnaire(): void {
    this.validationMessage.set(null);
    this.screenState.set('matching-in-progress');
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Visitor',
      tenant_user: 'Member',
      tenant_admin: 'Tenant admin',
      freelancer: 'Freelancer',
      business_partner: 'Business partner',
      event_organiser: 'Event organiser',
      platform_admin: 'Platform admin'
    };
    return labels[role];
  }

  roleTestId(): string {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-denied',
      tenant_user: 'role-tenant-user-profile',
      tenant_admin: 'role-tenant-admin-profile',
      freelancer: 'role-freelancer-profile',
      business_partner: 'role-business-partner-profile',
      event_organiser: 'role-event-organiser-profile',
      platform_admin: 'role-platform-admin-profile'
    };
    return ids[this.role()];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Sign in required',
      tenant_user: 'Profile questionnaire',
      tenant_admin: 'Profile review',
      freelancer: 'Freelancer profile setup',
      business_partner: 'Partner matching profile',
      event_organiser: 'Event organiser profile',
      platform_admin: 'Platform enrichment monitor'
    };
    return headings[this.role()];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return [
      'questionnaire',
      'processing',
      'validation',
      'matching-in-progress',
      'matching-partial',
      'matching-complete',
      'personalization-complete',
      'personalization-degraded'
    ].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
