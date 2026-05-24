import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'template_designer'
  | 'template_reviewer'
  | 'form_operator'
  | 'template_publisher'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'builder' | 'gallery' | 'submissions';

interface TemplateRow {
  name: string;
  fields: string;
  status: 'draft' | 'review' | 'published';
  version: string;
}

interface ReviewRow {
  check: string;
  owner: string;
  status: 'ready' | 'waiting' | 'locked';
}

interface UsageRow {
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
    'template_designer',
    'template_reviewer',
    'form_operator',
    'template_publisher',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('template_designer');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('builder');
  readonly moduleName = signal('Form Builder Templates');
  readonly activeFields = signal(8);

  readonly templateRows: TemplateRow[] = [
    { name: 'Customer intake', fields: 'name, priority, consent', status: 'published', version: 'v5' },
    { name: 'Partner onboarding', fields: 'company, region, owner', status: 'review', version: 'v2' },
    { name: 'Support request', fields: 'subject, urgency, details', status: 'draft', version: 'v1' }
  ];

  readonly reviewRows: ReviewRow[] = [
    { check: 'Schema validation', owner: 'template reviewer', status: 'ready' },
    { check: 'Layout constraints', owner: 'template designer', status: 'waiting' },
    { check: 'Publish package', owner: 'template publisher', status: 'locked' }
  ];

  readonly usageRows: UsageRow[] = [
    { metric: 'template uses', value: '12.8k', note: 'aggregate only' },
    { metric: 'median completion', value: '2m 41s', note: 'PII-free timing' },
    { metric: 'published templates', value: '7', note: 'version protected' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'תבניות טפסים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-form-builder-templates`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canDesign = computed(() => ['template_designer', 'template_publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canReview = computed(() => ['template_reviewer', 'template_publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canOperate = computed(() => ['form_operator', 'template_designer', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canPublish = computed(() => ['template_publisher', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) {
      this.role.set(roleParam);
    }

    if (stateParam && ['builder', 'gallery', 'submissions'].includes(stateParam)) {
      this.screenState.set(stateParam);
    }

    this.applyLanguage(langParam === 'he' ? 'he' : 'en');

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

  setActiveFields(value: number | string): void {
    this.activeFields.set(Number(value));
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
      'Template operations': 'ניהול תבניות',
      'Role': 'תפקיד',
      'Builder': 'בונה',
      'Gallery': 'גלריה',
      'Submissions': 'הגשות',
      'Published templates': 'תבניות שפורסמו',
      'Review checks': 'בדיקות סקירה',
      'Active fields': 'שדות פעילים'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) {
      return 'Public visitors can preview template examples only; schema edits, reviews, and submissions stay tenant-private.';
    }

    if (this.canPublish()) {
      return 'Template publishers approve immutable versions and publish reusable form packages with guarded schema compatibility.';
    }

    if (this.canReview()) {
      return 'Reviewers validate schema, layout, and read-only template mode before a template can be published.';
    }

    if (this.canDesign()) {
      return 'Designers compose fields, defaults, data slots, and layout constraints before review.';
    }

    if (this.canOperate()) {
      return 'Operators instantiate published templates and monitor aggregate submission outcomes.';
    }

    return 'Signed-in users browse the tenant gallery without editing private template definitions.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
