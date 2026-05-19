import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'freelancer'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'discovery' | 'membership' | 'feed';

interface GroupRow {
  name: string;
  type: string;
  status: string;
  detail: string;
}

interface FeedItem {
  title: string;
  score: string;
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
    'public_marketplace_visitor',
    'tenant_user',
    'tenant_admin',
    'freelancer',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('discovery');
  readonly moduleName = signal('User Groups Communities');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'קהילות וקבוצות משתמשים' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-groups',
      public_marketplace_visitor: 'role-public-marketplace-visitor-groups',
      tenant_user: 'role-tenant-user-groups',
      tenant_admin: 'role-tenant-admin-groups',
      freelancer: 'role-freelancer-groups',
      platform_admin: 'role-platform-admin-groups'
    };
    return ids[this.role()];
  });

  readonly groups: GroupRow[] = [
    { name: 'Launch Builders', type: 'Open group', status: 'Join available', detail: 'Members can enter immediately and see the starter feed.' },
    { name: 'Founders Circle', type: 'Invite only', status: 'Approval required', detail: 'Requests wait for an admin decision before content opens.' },
    { name: 'Freelancer Studio', type: 'Tiered group', status: 'Premium path', detail: 'Feed items adjust to the member tier kept on the server.' }
  ];

  readonly feedItems: FeedItem[] = [
    { title: 'Welcome thread', score: '0.86', detail: 'High fit for a new active member' },
    { title: 'Weekly group brief', score: '0.74', detail: 'Fresh update from the community lead' },
    { title: 'Mentor room', score: '0.71', detail: 'Available after membership activation' }
  ];

  readonly auditRows = [
    { label: 'Join request', value: 'Pending', detail: 'Invite-only access waits for review' },
    { label: 'Membership', value: 'Active', detail: 'Open groups activate immediately' },
    { label: 'Tier', value: 'Server set', detail: 'The tier does not come from the form' },
    { label: 'Feed', value: 'Unlocked', detail: 'Content appears only after activation' }
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
      anonymous: 'Anonymous visitor',
      public_marketplace_visitor: 'Marketplace visitor',
      tenant_user: 'Group member',
      tenant_admin: 'Tenant admin',
      freelancer: 'Freelancer member',
      platform_admin: 'Platform admin'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Public discovery',
      public_marketplace_visitor: 'Marketplace preview',
      tenant_user: 'Your group membership',
      tenant_admin: 'Community approvals',
      freelancer: 'Your member feed',
      platform_admin: 'Platform community operations'
    };
    return headings[this.role()];
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['discovery', 'membership', 'feed'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
