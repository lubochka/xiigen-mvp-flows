import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'freelancer'
  | 'business_partner'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'request' | 'connections' | 'feed';

interface RequestRow {
  label: string;
  value: string;
  detail: string;
}

interface ConnectionRow {
  name: string;
  strength: string;
  zone: string;
  detail: string;
}

interface FeedRow {
  title: string;
  score: string;
  placement: string;
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
    'business_partner',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('request');
  readonly moduleName = signal('Friend Request Social Feed');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'בקשות חברות ועדכון חברתי' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-friend-feed',
      public_marketplace_visitor: 'role-public-marketplace-visitor-friend-feed',
      tenant_user: 'role-tenant-user-friend-feed',
      tenant_admin: 'role-tenant-admin-friend-feed',
      freelancer: 'role-freelancer-friend-feed',
      business_partner: 'role-business-partner-friend-feed',
      platform_admin: 'role-platform-admin-friend-feed'
    };
    return ids[this.role()];
  });

  readonly requestRows: RequestRow[] = [
    { label: 'Privacy', value: 'Checked inline', detail: 'A blocked pair stops before a request record is created.' },
    { label: 'Idempotency', value: 'Single result', detail: 'Repeated keys return the stored response without duplicate side effects.' },
    { label: 'Concurrency', value: 'One winner', detail: 'Parallel requests for the same pair cannot create two pending records.' },
    { label: 'Timeout', value: 'Retry ready', detail: 'A timeout leaves a retained event for recovery.' }
  ];

  readonly connections: ConnectionRow[] = [
    { name: 'Maya Chen', strength: '0.92', zone: 'top 20%', detail: 'Shared groups, recent event overlap, and matching questionnaire answers.' },
    { name: 'Noam Levy', strength: '0.67', zone: 'middle 40%', detail: 'Moderate activity overlap with two recent marketplace interactions.' },
    { name: 'Dana Bar', strength: '0.31', zone: 'bottom 40%', detail: 'Weak signal retained without disappearing from the graph.' }
  ];

  readonly feedRows: FeedRow[] = [
    { title: 'Maya posted a launch checklist', score: '0.92', placement: 'Top rail', detail: 'Strong connections receive twenty items in the most visible area.' },
    { title: 'Noam joined the product clinic', score: '0.67', placement: 'Middle rail', detail: 'Medium connections receive a smaller, balanced window.' },
    { title: 'Dana added a quiet profile update', score: '0.00', placement: 'Retained', detail: 'A zero score remains valid evidence and is not filtered out.' }
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
      tenant_user: 'Tenant user',
      tenant_admin: 'Tenant admin',
      freelancer: 'Freelancer',
      business_partner: 'Business partner',
      platform_admin: 'Platform admin'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Public friendship preview',
      public_marketplace_visitor: 'Marketplace social proof',
      tenant_user: 'Your social feed',
      tenant_admin: 'Tenant social operations',
      freelancer: 'Freelancer network',
      business_partner: 'Partner relationship graph',
      platform_admin: 'Platform social oversight'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Request': 'בקשה',
      'Connections': 'קשרים',
      'Feed': 'עדכון',
      'Friend request': 'בקשת חברות',
      'Social graph': 'מפת קשרים',
      'Personal feed': 'עדכון אישי',
      'Role': 'תפקיד',
      'Request state': 'מצב בקשה',
      'Connection strength': 'חוזק קשר',
      'Feed delivery': 'מסירת עדכון'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['request', 'connections', 'feed'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
