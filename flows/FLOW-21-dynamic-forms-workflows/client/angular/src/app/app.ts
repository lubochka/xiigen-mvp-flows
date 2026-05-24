import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'schema' | 'workflow' | 'analytics';

interface FieldRow {
  key: string;
  type: string;
  visibleTo: string;
  state: 'ready' | 'conditional' | 'private';
}

interface WorkflowRow {
  trigger: string;
  branch: string;
  next: string;
  state: 'active' | 'waiting' | 'locked';
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
    'freelancer',
    'business_partner',
    'event_organiser',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('schema');
  readonly moduleName = signal('Dynamic Forms Workflows');
  readonly requiredFields = signal(3);

  readonly fieldRows: FieldRow[] = [
    { key: 'display name', type: 'text', visibleTo: 'members and partners', state: 'ready' },
    { key: 'priority', type: 'choice', visibleTo: 'all signed-in roles', state: 'conditional' },
    { key: 'admin notes', type: 'text', visibleTo: 'admins only', state: 'private' }
  ];

  readonly workflowRows: WorkflowRow[] = [
    { trigger: 'new submission', branch: 'urgent', next: 'accelerated review', state: 'active' },
    { trigger: 'new submission', branch: 'standard', next: 'standard review', state: 'waiting' },
    { trigger: 'dispatch', branch: 'repeat attempt', next: 'replay prior result', state: 'locked' }
  ];

  readonly metricRows: MetricRow[] = [
    { metric: 'completion rate', value: '87%', note: 'aggregate only' },
    { metric: 'average time', value: '42s', note: 'no respondent details' },
    { metric: 'workflow dispatches', value: '18', note: 'append-only evidence' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'טפסים ותהליכים דינמיים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-dynamic-forms-workflows`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canAuthor = computed(() => ['tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canSubmit = computed(() => ['tenant_user', 'freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canDispatch = computed(() => ['tenant_admin', 'platform_admin'].includes(this.role()));
  readonly summary = computed(() => {
    if (this.publicView()) {
      return 'Preview form capabilities without seeing private tenant responses.';
    }
    if (this.canAuthor()) {
      return 'Publish schema versions, test conditional fields, and dispatch workflow branches safely.';
    }
    return 'Complete tenant forms with only the fields your role is allowed to see.';
  });

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

  setRequiredFields(value: number): void {
    this.requiredFields.set(value);
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
      freelancer: 'Specialist',
      business_partner: 'Partner',
      event_organiser: 'Event organiser',
      tenant_admin: 'Workspace admin',
      platform_admin: 'Platform admin'
    };
    return labels[role];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Form operations': 'ניהול טפסים',
      'Role': 'תפקיד',
      'Schema': 'מבנה',
      'Workflow': 'תהליך',
      'Analytics': 'מדדים',
      'Published versions': 'גרסאות שפורסמו',
      'Submissions today': 'שליחות היום',
      'Workflow dispatches': 'הרצות תהליך',
      'Publish schema': 'פרסם מבנה',
      'Submit form': 'שלח טופס',
      'Dispatch workflow': 'הפעל תהליך',
      'Preview only': 'תצוגה בלבד',
      'No private submission data is visible here.': 'נתוני שליחה פרטיים אינם מוצגים כאן.'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['schema', 'workflow', 'analytics'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
