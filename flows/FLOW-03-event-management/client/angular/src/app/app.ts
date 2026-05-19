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
  | 'creation'
  | 'validation'
  | 'created'
  | 'registration'
  | 'waitlist'
  | 'promotion'
  | 'promotion-rejected'
  | 'analytics';

interface EventCard {
  id: string;
  title: string;
  date: string;
  capacityLabel: string;
  status: string;
}

interface AudienceRow {
  segment: string;
  score: number;
  placement: string;
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
  readonly role = signal<ViewerRole>('event_organiser');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('creation');
  readonly moduleName = signal('Event Management');
  readonly title = signal('Annual Tech Summit');
  readonly capacity = signal('100');
  readonly unlimited = signal(false);
  readonly paid = signal(false);
  readonly privateEvent = signal(false);
  readonly validationMessage = signal<string | null>(null);

  readonly isRtl = computed(() => this.language() === 'he');
  readonly isProtected = computed(() => this.role() === 'anonymous');
  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניהול אירועים' : this.moduleName());
  readonly canCreate = computed(() => this.role() === 'event_organiser' || this.role() === 'tenant_admin' || this.role() === 'platform_admin');
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-denied',
      tenant_user: 'role-tenant-user-event',
      tenant_admin: 'role-tenant-admin-event',
      freelancer: 'role-freelancer-event',
      business_partner: 'role-business-partner-event',
      event_organiser: 'role-event-organiser-event',
      platform_admin: 'role-platform-admin-event'
    };
    return ids[this.role()];
  });

  readonly events: EventCard[] = [
    { id: 'evt-001', title: 'Annual Tech Summit', date: 'Jun 15', capacityLabel: '142 / 200', status: 'Promoted' },
    { id: 'evt-002', title: 'Product Design Workshop', date: 'Jul 1', capacityLabel: '30 / 30', status: 'Waitlist' },
    { id: 'evt-003', title: 'Open Source Hackathon', date: 'Aug 10', capacityLabel: '89 registered (unlimited)', status: 'Open' }
  ];

  readonly audienceRows: AudienceRow[] = [
    { segment: 'Technology founders nearby', score: 91, placement: 'Top 3' },
    { segment: 'Members free that evening', score: 78, placement: 'Top 10' },
    { segment: 'Partner network matches', score: 66, placement: 'Within view' }
  ];

  readonly notifications = ['In-app', 'Push', 'Email digest'];

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

  updateTitle(value: string): void {
    this.title.set(value);
  }

  updateCapacity(value: string): void {
    this.capacity.set(value);
  }

  setUnlimited(value: boolean): void {
    this.unlimited.set(value);
    if (value) {
      this.capacity.set('');
    }
  }

  setPaid(value: boolean): void {
    this.paid.set(value);
  }

  setPrivateEvent(value: boolean): void {
    this.privateEvent.set(value);
  }

  submitEvent(): void {
    if (!this.title().trim()) {
      this.screenState.set('validation');
      this.validationMessage.set('Please add an event title before publishing.');
      return;
    }

    this.validationMessage.set(null);
    this.screenState.set('created');
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

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Sign in required',
      tenant_user: 'Upcoming events',
      tenant_admin: 'Event moderation',
      freelancer: 'Service opportunities',
      business_partner: 'Sponsor-ready events',
      event_organiser: 'Create an event',
      platform_admin: 'Platform event operations'
    };
    return headings[this.role()];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return [
      'creation',
      'validation',
      'created',
      'registration',
      'waitlist',
      'promotion',
      'promotion-rejected',
      'analytics'
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
