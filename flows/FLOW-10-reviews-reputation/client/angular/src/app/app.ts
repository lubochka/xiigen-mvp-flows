import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'freelancer'
  | 'business_partner'
  | 'tenant_admin'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'submission' | 'moderation' | 'response';

interface ReviewCard {
  title: string;
  target: string;
  rating: string;
  status: string;
  detail: string;
  badge: string;
}

interface SignalRow {
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
  readonly roles: ViewerRole[] = [
    'anonymous',
    'public_marketplace_visitor',
    'tenant_user',
    'freelancer',
    'business_partner',
    'tenant_admin',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('submission');
  readonly moduleName = signal('Reviews Reputation');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ביקורות ומוניטין' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-reviews-reputation',
      public_marketplace_visitor: 'role-public-marketplace-visitor-reviews-reputation',
      tenant_user: 'role-tenant-user-reviews-reputation',
      freelancer: 'role-freelancer-reviews-reputation',
      business_partner: 'role-business-partner-reviews-reputation',
      tenant_admin: 'role-tenant-admin-reviews-reputation',
      platform_admin: 'role-platform-admin-reviews-reputation',
      platform_support: 'role-platform-support-reviews-reputation'
    };
    return ids[this.role()];
  });

  readonly reviewCards: ReviewCard[] = [
    {
      title: 'Verified workshop review',
      target: 'Founder breakfast',
      rating: '5.0',
      status: 'Eligible',
      detail: 'Only a confirmed attendee can submit; the review identifier is derived by the platform and cannot be supplied by the browser.',
      badge: 'Check-in confirmed'
    },
    {
      title: 'Supplier delivery note',
      target: 'North Hall catering',
      rating: '4.0',
      status: 'Purchase matched',
      detail: 'A completed purchase can also prove eligibility, keeping service reviews aligned with the transaction trail.',
      badge: 'Purchase matched'
    },
    {
      title: 'Community profile feedback',
      target: 'Mentor profile',
      rating: '3.0',
      status: 'Needs moderation',
      detail: 'Every submitted review enters the moderation path before it can affect the public score.',
      badge: 'Review pending'
    }
  ];

  readonly submissionSignals: SignalRow[] = [
    { label: 'Eligibility', value: 'Before writes', detail: 'Attendance or purchase confirmation is checked before validation, retry keys, audit, or review storage.' },
    { label: 'Rating boundary', value: '1 to 5 only', detail: 'Invalid ratings return field-level guidance and leave no partial review trail behind.' },
    { label: 'Audit trail', value: 'No review text', detail: 'Audit records keep actor, target, and status details while private review text stays out.' },
    { label: 'Retry safety', value: 'One review', detail: 'Repeated submission with the same key returns the first accepted result.' }
  ];

  readonly moderationSignals: SignalRow[] = [
    { label: 'Pass', value: 'Published', detail: 'High-confidence reviews become public and recalculate the target reputation score.' },
    { label: 'Reject', value: 'Rejected', detail: 'Low-confidence reviews are rejected without contributing to the score.' },
    { label: 'Uncertain', value: 'Human review', detail: 'Borderline confidence sends the item to a person instead of rejecting it automatically.' },
    { label: 'Score', value: 'Published only', detail: 'Retracted and rejected reviews are excluded from weighted reputation summaries.' }
  ];

  readonly responseSignals: SignalRow[] = [
    { label: 'Ownership', value: 'Checked first', detail: 'Only the reviewed owner or an authorized operator can answer a review.' },
    { label: 'One response', value: 'Revision gated', detail: 'A second response is blocked unless the previous answer was rejected for policy and needs revision.' },
    { label: 'Private text', value: 'Not broadcast', detail: 'Response text is saved privately and never appears in audit or event payloads.' },
    { label: 'Score impact', value: 'No change', detail: 'Owner responses do not change the public reputation score.' }
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
      anonymous: 'Guest visitor',
      public_marketplace_visitor: 'Marketplace visitor',
      tenant_user: 'Reviewer',
      freelancer: 'Portfolio reviewer',
      business_partner: 'Reviewed owner',
      tenant_admin: 'Tenant admin',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview reputation signals',
      public_marketplace_visitor: 'Compare public scores',
      tenant_user: 'Submit an eligible review',
      freelancer: 'Show trusted profile proof',
      business_partner: 'Respond as the reviewed owner',
      tenant_admin: 'Moderate review quality',
      platform_admin: 'Inspect platform controls',
      platform_support: 'Support without tenant leakage'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const ctas: Record<ViewerRole, string> = {
      anonymous: 'Sign in to review',
      public_marketplace_visitor: 'Open public score',
      tenant_user: 'Submit review',
      freelancer: 'View profile trust',
      business_partner: 'Respond to review',
      tenant_admin: 'Open moderation',
      platform_admin: 'Inspect audit',
      platform_support: 'Open support view'
    };
    return ctas[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Reviews Reputation': 'ביקורות ומוניטין',
      'Submission': 'שליחה',
      'Moderation': 'בקרה',
      'Response': 'תגובה',
      'Role': 'תפקיד',
      'Eligible reviews': 'ביקורות זכאיות',
      'Moderation paths': 'מסלולי בקרה',
      'Owner response': 'תגובת בעלים'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['submission', 'moderation', 'response'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
