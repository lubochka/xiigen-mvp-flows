import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'referral_user'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'tenant_admin'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'general' | 'members' | 'billing' | 'advanced';

interface SettingsTab {
  id: ScreenState;
  label: string;
  summary: string;
}

interface MemberRow {
  name: string;
  role: string;
  status: string;
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
    'freelancer',
    'business_partner',
    'event_organiser',
    'tenant_admin',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('general');
  readonly moduleName = signal('Workspace Settings');
  readonly workspaceName = signal('Acme Workspace');
  readonly inviteEmail = signal('new.member@example.test');
  readonly billingContact = signal('finance@example.test');
  readonly dangerConfirmation = signal('');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'הגדרות סביבת עבודה' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-saas-multi-tenancy`);
  readonly canEdit = computed(() => ['tenant_admin', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly dangerEnabled = computed(() => this.dangerConfirmation() === 'SUSPEND WORKSPACE');
  readonly currentTab = computed(() => this.tabs.find(tab => tab.id === this.screenState()) ?? this.tabs[0]);

  readonly tabs: SettingsTab[] = [
    { id: 'general', label: 'General', summary: 'Workspace name, region, default language, and lifecycle state.' },
    { id: 'members', label: 'Members', summary: 'Invite, resend, cancel, and role-change actions stay tenant-scoped.' },
    { id: 'billing', label: 'Billing', summary: 'Subscription tier, quota counters, and billing portal access.' },
    { id: 'advanced', label: 'Advanced', summary: 'Integrations, locked keys, and guarded lifecycle actions.' }
  ];

  readonly members: MemberRow[] = [
    { name: 'Maya Cohen', role: 'Owner', status: 'Active' },
    { name: 'Jon Bell', role: 'Finance approver', status: 'Invited' },
    { name: 'Rina Shah', role: 'Workspace admin', status: 'Active' }
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

  setWorkspaceName(value: string): void {
    this.workspaceName.set(value);
  }

  setInviteEmail(value: string): void {
    this.inviteEmail.set(value);
  }

  setBillingContact(value: string): void {
    this.billingContact.set(value);
  }

  setDangerConfirmation(value: string): void {
    this.dangerConfirmation.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest preview',
      tenant_user: 'Workspace member',
      referral_user: 'Referral viewer',
      freelancer: 'Contract contributor',
      business_partner: 'Business partner',
      event_organiser: 'Event organiser',
      tenant_admin: 'Workspace admin',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview workspace settings without private records',
      tenant_user: 'Review the workspace details visible to members',
      referral_user: 'View shared workspace context',
      freelancer: 'Check assigned access and integration status',
      business_partner: 'Review billing and integration readiness',
      event_organiser: 'Confirm event team workspace access',
      tenant_admin: 'Edit settings, members, billing, and lifecycle controls',
      platform_admin: 'Audit workspace isolation and master tenant safeguards',
      platform_support: 'Recover tenant lifecycle and quota issues'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Workspace Settings': 'הגדרות סביבת עבודה',
      'General': 'כללי',
      'Members': 'חברים',
      'Billing': 'חיוב',
      'Advanced': 'מתקדם',
      'Role': 'תפקיד',
      'Workspace name': 'שם סביבת העבודה',
      'Invite member': 'הזמנת חבר',
      'Billing contact': 'איש קשר לחיוב',
      'Danger zone': 'אזור רגיש',
      'Suspend workspace': 'השהיית סביבת עבודה'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['general', 'members', 'billing', 'advanced'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => part.toLowerCase() === 'saas' ? 'SaaS' : `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
