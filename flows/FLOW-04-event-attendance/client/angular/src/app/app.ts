import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'referral_user'
  | 'tenant_admin'
  | 'business_partner'
  | 'event_organiser'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'rsvp' | 'waitlist' | 'checkin' | 'feedback' | 'dashboard';

interface AttendanceRow {
  attendee: string;
  state: string;
  detail: string;
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
    'referral_user',
    'tenant_admin',
    'business_partner',
    'event_organiser',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('rsvp');
  readonly moduleName = signal('Event Attendance');
  readonly selectedAttendee = signal('Maya Chen');
  readonly selectedEvent = signal('Community Launch Night');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניהול נוכחות לאירוע' : this.moduleName());
  readonly isProtected = computed(() => this.role() === 'anonymous');
  readonly isRtl = computed(() => this.language() === 'he');
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-denied',
      tenant_user: 'role-tenant-user-attendance',
      referral_user: 'role-referral-user-attendance',
      tenant_admin: 'role-tenant-admin-attendance',
      business_partner: 'role-business-partner-attendance',
      event_organiser: 'role-event-organiser-attendance',
      platform_admin: 'role-platform-admin-attendance'
    };
    return ids[this.role()];
  });

  readonly rows: AttendanceRow[] = [
    { attendee: 'Maya Chen', state: 'Confirmed', detail: 'Seat held from direct RSVP' },
    { attendee: 'Ari Levin', state: 'Waitlisted', detail: 'Next FIFO promotion candidate' },
    { attendee: 'Noa Bar', state: 'Checked in', detail: 'Arrival signal sent to rewards' },
    { attendee: 'Samir Haddad', state: 'Feedback open', detail: 'Window closes in 68 hours' }
  ];

  readonly checkpoints = [
    'One RSVP write before queue dispatch',
    'Capacity zero routes to waitlist',
    'Paid RSVP waits for payment confirmation',
    'Feedback opens only after event ended'
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
      anonymous: 'Visitor',
      tenant_user: 'Member',
      referral_user: 'Referral guest',
      tenant_admin: 'Tenant admin',
      business_partner: 'Business partner',
      event_organiser: 'Event organiser',
      platform_admin: 'Platform admin'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Sign in required',
      tenant_user: 'Your RSVP',
      referral_user: 'Referral guest entry',
      tenant_admin: 'Tenant attendance control',
      business_partner: 'Partner attendance view',
      event_organiser: 'Check-in desk',
      platform_admin: 'Platform attendance operations'
    };
    return headings[this.role()];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['rsvp', 'waitlist', 'checkin', 'feedback', 'dashboard'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
