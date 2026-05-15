import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'anonymous' | 'tenant_user' | 'tenant_admin' | 'platform_admin' | 'platform_support';
type Language = 'en' | 'he';
type JourneyState = 'registration' | 'pending' | 'verify-token' | 'resend' | 'onboarding' | 'sso';

interface JourneyStep {
  id: JourneyState;
  label: string;
}

interface RegistrationRecord {
  email: string;
  status: 'Pending verification' | 'Verified' | 'Onboarding complete';
  source: 'Email' | 'Single sign-on';
  updated: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = ['anonymous', 'tenant_user', 'tenant_admin', 'platform_admin', 'platform_support'];
  readonly role = signal<ViewerRole>('anonymous');
  readonly language = signal<Language>('en');
  readonly journeyState = signal<JourneyState>('registration');
  readonly moduleName = signal('User Registration');
  readonly email = signal('new.member@example.com');
  readonly password = signal('local-demo-password');
  readonly token = signal('sample-verification-token');
  readonly message = signal<string | null>(null);

  readonly steps: JourneyStep[] = [
    { id: 'registration', label: 'Registration' },
    { id: 'pending', label: 'Pending email' },
    { id: 'verify-token', label: 'Verify token' },
    { id: 'resend', label: 'Resend' },
    { id: 'onboarding', label: 'Onboarding' },
    { id: 'sso', label: 'Single sign-on' }
  ];

  readonly records: RegistrationRecord[] = [
    { email: 'new.member@example.com', status: 'Pending verification', source: 'Email', updated: '2 min ago' },
    { email: 'verified.member@example.com', status: 'Verified', source: 'Email', updated: '18 min ago' },
    { email: 'sso.member@example.com', status: 'Onboarding complete', source: 'Single sign-on', updated: '1 hour ago' }
  ];

  readonly localizedTitle = computed(() => this.language() === 'he' ? 'רישום משתמשים' : this.moduleName());
  readonly isRtl = computed(() => this.language() === 'he');
  readonly signedIn = computed(() => this.role() !== 'anonymous');

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
    const params = new URLSearchParams(this.document.defaultView?.location.search ?? '');
    const requestedRole = params.get('role');
    const requestedState = params.get('state');
    const requestedModule = params.get('module');
    const requestedLanguage: Language = params.get('lang') === 'he' ? 'he' : 'en';

    if (this.isViewerRole(requestedRole)) {
      this.role.set(requestedRole);
    }
    if (this.isJourneyState(requestedState)) {
      this.journeyState.set(requestedState);
    }
    if (requestedModule) {
      this.moduleName.set(this.toTitle(requestedModule));
    }
    this.applyLanguage(requestedLanguage);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
    this.message.set(null);
  }

  setJourneyState(value: JourneyState): void {
    this.journeyState.set(value);
    this.message.set(null);
  }

  updateEmail(value: string): void {
    this.email.set(value);
  }

  updatePassword(value: string): void {
    this.password.set(value);
  }

  updateToken(value: string): void {
    this.token.set(value);
  }

  submitRegistration(): void {
    this.journeyState.set('pending');
    this.message.set('Registration is saved. A verification email has been queued for delivery.');
  }

  verifyToken(): void {
    this.journeyState.set('onboarding');
    this.message.set('Email verified. Onboarding materials are now available.');
  }

  resendEmail(): void {
    this.message.set('A fresh verification email was requested. Rate limits are checked before delivery.');
  }

  completeOnboarding(): void {
    this.message.set('Workspace setup, tutorial, and community invitation are complete.');
  }

  startSso(): void {
    this.journeyState.set('sso');
    this.message.set('Single sign-on continues through the approved identity provider.');
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
      platform_admin: 'Platform admin',
      platform_support: 'Support'
    };
    return labels[role];
  }

  stateLabel(state: JourneyState): string {
    return this.steps.find(step => step.id === state)?.label ?? 'Registration';
  }

  hebrewLabel(state: JourneyState): string {
    const labels: Record<JourneyState, string> = {
      registration: 'הרשמה',
      pending: 'ממתין לאימייל',
      'verify-token': 'אימות קישור',
      resend: 'שליחה חוזרת',
      onboarding: 'קליטה',
      sso: 'כניסה אחידה'
    };
    return labels[state];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isJourneyState(value: string | null): value is JourneyState {
    return this.steps.some(step => step.id === value);
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
