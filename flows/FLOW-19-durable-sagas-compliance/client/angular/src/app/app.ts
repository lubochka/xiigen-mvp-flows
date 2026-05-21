import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'tenant_admin'
  | 'tenant_consumer'
  | 'tenant_ops'
  | 'saga_operator'
  | 'compliance_officer'
  | 'platform_ops'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'operations' | 'sagas' | 'compliance';

interface SagaStep {
  label: string;
  state: 'done' | 'active' | 'future' | 'compensating' | 'failed';
  timestamp: string;
}

interface SagaCard {
  id: string;
  title: string;
  state: 'active' | 'completed' | 'compensating' | 'failed-final';
  currentStage: string;
  owner: string;
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
    'tenant_consumer',
    'tenant_ops',
    'saga_operator',
    'compliance_officer',
    'platform_ops',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_ops');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('operations');
  readonly moduleName = signal('Durable Sagas Compliance');
  readonly selectedSaga = signal('payout-7421');
  readonly exportFormat = signal('csv');

  readonly progress: SagaStep[] = [
    { label: 'Request', state: 'done', timestamp: '09:04 UTC' },
    { label: 'Verify', state: 'done', timestamp: '09:11 UTC' },
    { label: 'Approve', state: 'active', timestamp: 'in progress' },
    { label: 'Process', state: 'future', timestamp: 'waiting' },
    { label: 'Paid', state: 'future', timestamp: 'waiting' }
  ];

  readonly sagaCards: SagaCard[] = [
    { id: 'payout-7421', title: 'Creator payout', state: 'active', currentStage: 'Approve', owner: 'Maya Chen' },
    { id: 'import-1890', title: 'Partner import', state: 'compensating', currentStage: 'Rollback verify', owner: 'Ops queue' },
    { id: 'approval-4408', title: 'Manual approval chain', state: 'completed', currentStage: 'Complete', owner: 'Compliance desk' }
  ];

  readonly auditRows = [
    { time: '09:04 UTC', saga: 'payout-7421', event: 'Saga started', actor: 'tenant ops', retention: '2033-05-21' },
    { time: '09:11 UTC', saga: 'payout-7421', event: 'Verification stored', actor: 'platform', retention: '2033-05-21' },
    { time: '09:18 UTC', saga: 'import-1890', event: 'Compensation started', actor: 'system', retention: '2033-05-21' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מעקב סאגות וביקורת' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-durable-sagas-compliance`);
  readonly selected = computed(() => this.sagaCards.find(card => card.id === this.selectedSaga()) ?? this.sagaCards[0]);
  readonly canOperate = computed(() => ['tenant_admin', 'tenant_ops', 'saga_operator', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly canAudit = computed(() => ['compliance_officer', 'platform_ops', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly userFacing = computed(() => ['tenant_user', 'tenant_consumer', 'anonymous'].includes(this.role()));

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

  setSelectedSaga(value: string): void {
    this.selectedSaga.set(value);
  }

  setExportFormat(value: string): void {
    this.exportFormat.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest status viewer',
      tenant_user: 'Workspace member',
      tenant_admin: 'Workspace admin',
      tenant_consumer: 'Operation owner',
      tenant_ops: 'Tenant operations',
      saga_operator: 'Saga operator',
      compliance_officer: 'Compliance officer',
      platform_ops: 'Platform operations',
      platform_admin: 'Platform admin',
      platform_support: 'Support specialist'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview a safe operation status without private audit data',
      tenant_user: 'Track your own long-running operation',
      tenant_admin: 'Monitor tenant saga runs and retry failed work',
      tenant_consumer: 'See clear progress and estimated completion',
      tenant_ops: 'Operate active sagas with retry and rollback controls',
      saga_operator: 'Repair failed runs with visible compensation order',
      compliance_officer: 'Export immutable audit trails',
      platform_ops: 'Review retention gates and platform alerts',
      platform_admin: 'Govern saga safety, retention, and audit exports',
      platform_support: 'Investigate stuck operations with safe evidence'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Durable Sagas Compliance': 'מעקב סאגות וביקורת',
      'Role': 'תפקיד',
      'Operations': 'פעולות',
      'Saga dashboard': 'לוח סאגות',
      'Compliance audit': 'ביקורת',
      'Estimated completion': 'הערכת סיום',
      'Retry failed step': 'נסה שוב',
      'Rollback': 'חזרה בטוחה',
      'Export': 'ייצוא',
      'No action needed': 'אין צורך בפעולה',
      'Needs attention': 'דורש טיפול'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['operations', 'sagas', 'compliance'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
