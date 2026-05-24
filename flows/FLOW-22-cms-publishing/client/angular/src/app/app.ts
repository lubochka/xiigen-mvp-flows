import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'content_author'
  | 'content_editor'
  | 'legal_reviewer'
  | 'publisher'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'drafts' | 'approvals' | 'analytics';

interface ContentRow {
  title: string;
  slug: string;
  status: 'draft' | 'review' | 'published';
  version: string;
}

interface ApprovalRow {
  stage: string;
  owner: string;
  status: 'ready' | 'waiting' | 'locked';
}

interface MetricRow {
  metric: string;
  value: string;
  note: string;
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
    'content_author',
    'content_editor',
    'legal_reviewer',
    'publisher',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('publisher');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('drafts');
  readonly moduleName = signal('CMS Publishing');
  readonly scheduledItems = signal(5);

  readonly contentRows: ContentRow[] = [
    { title: 'Launch notes', slug: '/launch-notes', status: 'published', version: 'v4' },
    { title: 'Pricing update', slug: '/pricing-update', status: 'review', version: 'v2' },
    { title: 'Partner guide', slug: '/partner-guide', status: 'draft', version: 'v1' }
  ];

  readonly approvalRows: ApprovalRow[] = [
    { stage: 'Editor review', owner: 'content editor', status: 'ready' },
    { stage: 'Legal review', owner: 'legal reviewer', status: 'waiting' },
    { stage: 'Final approval', owner: 'publisher', status: 'locked' }
  ];

  readonly metricRows: MetricRow[] = [
    { metric: 'published views', value: '18.4k', note: 'aggregate only' },
    { metric: 'approval time', value: '3h 12m', note: 'team-level trend' },
    { metric: 'scheduled releases', value: '5', note: 'durable timers' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'פרסום תוכן חכם' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-cms-publishing`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canAuthor = computed(() => ['content_author', 'content_editor', 'publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canApprove = computed(() => ['content_editor', 'legal_reviewer', 'publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canPublish = computed(() => ['publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) {
      this.role.set(roleParam);
    }

    if (stateParam && ['drafts', 'approvals', 'analytics'].includes(stateParam)) {
      this.screenState.set(stateParam);
    }

    if (langParam === 'he' || langParam === 'en') {
      this.applyLanguage(langParam);
    } else {
      this.applyLanguage('en');
    }

    if (moduleParam) {
      this.moduleName.set(moduleParam);
    }
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  setScheduledItems(value: number | string): void {
    this.scheduledItems.set(Number(value));
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }

    const dictionary: Record<string, string> = {
      'Editorial operations': 'פעילות עריכה',
      'Role': 'תפקיד',
      'Drafts': 'טיוטות',
      'Approvals': 'אישורים',
      'Analytics': 'מדדים',
      'Published versions': 'גרסאות שפורסמו',
      'Pending approvals': 'אישורים ממתינים',
      'Scheduled releases': 'פרסומים מתוזמנים'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) {
      return 'Public readers see published pages only; drafts, approvals, and schedules stay tenant-private.';
    }

    if (this.canPublish()) {
      return 'Publishers manage immutable versions, approval gates, scheduled dispatch, and aggregate content performance.';
    }

    if (this.canApprove()) {
      return 'Reviewers see the approval lane that matches their role and cannot bypass earlier stages.';
    }

    if (this.canAuthor()) {
      return 'Authors prepare draft content and submit it into the protected review pipeline.';
    }

    return 'Signed-in readers can browse published content without seeing editorial controls.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
