import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_admin'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'submission' | 'approval' | 'dag';

interface SchemaCard {
  title: string;
  version: string;
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
    'tenant_admin',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('submission');
  readonly moduleName = signal('Schema Registry DAG');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מאגר סכמות ומפת תלות' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-schema-registry-dag',
      public_marketplace_visitor: 'role-public-marketplace-visitor-schema-registry-dag',
      tenant_admin: 'role-tenant-admin-schema-registry-dag',
      platform_admin: 'role-platform-admin-schema-registry-dag',
      platform_support: 'role-platform-support-schema-registry-dag'
    };
    return ids[this.role()];
  });

  readonly schemaCards: SchemaCard[] = [
    {
      title: 'Profile contract',
      version: '1.2.0',
      status: 'Published',
      detail: 'Compatible fields publish after version, dependency, and compatibility checks complete.',
      badge: 'Additive change'
    },
    {
      title: 'Order event',
      version: '2.0.0',
      status: 'Approval required',
      detail: 'Breaking changes are held for governance and never become active until approved.',
      badge: 'Breaking gate'
    },
    {
      title: 'Ledger projection',
      version: '1.0.3',
      status: 'DAG rebuilt',
      detail: 'Dependencies render as one tenant-scoped graph with cached JSON and Mermaid views.',
      badge: 'Cycle-free'
    }
  ];

  readonly submissionSignals: SignalRow[] = [
    { label: 'Version gate', value: 'Monotonic', detail: 'A new schema version must be higher than the active tenant version.' },
    { label: 'Dependency gate', value: 'Three states', detail: 'Cycle detection uses WHITE, GRAY, and BLACK traversal so diamonds pass and loops fail.' },
    { label: 'Compatibility', value: 'Classified', detail: 'Additive and compatible changes publish; breaking changes route to approval.' },
    { label: 'Retry safety', value: 'One version', detail: 'Repeated requests with one key replay the first registry response.' }
  ];

  readonly approvalSignals: SignalRow[] = [
    { label: 'Approve', value: 'Publish', detail: 'Approved breaking changes become active through the same publication and topology path.' },
    { label: 'Reject', value: 'Stop', detail: 'Rejected changes remain in the approval record and do not publish a schema version.' },
    { label: 'Defer', value: 'Timed window', detail: 'Open approval items keep their tenant window from runtime configuration.' },
    { label: 'History', value: 'Append only', detail: 'Published decisions append a full version snapshot for support and audit.' }
  ];

  readonly dagSignals: SignalRow[] = [
    { label: 'Topology', value: 'Tenant scoped', detail: 'Every node and edge is rebuilt from the current tenant registry only.' },
    { label: 'Render', value: 'JSON or Mermaid', detail: 'Operators can inspect the graph without reading raw service or event code.' },
    { label: 'Cache', value: 'TTL governed', detail: 'DAG render cache is operationally tunable while graph correctness stays fixed.' },
    { label: 'Validation', value: 'Pinned version', detail: 'Document validation uses the selected schema version, not whatever is newest later.' }
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
      anonymous: 'Guest observer',
      public_marketplace_visitor: 'Marketplace visitor',
      tenant_admin: 'Tenant admin',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview registry status',
      public_marketplace_visitor: 'Inspect public package contracts',
      tenant_admin: 'Submit tenant schemas',
      platform_admin: 'Govern breaking changes',
      platform_support: 'Troubleshoot dependency graphs'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const ctas: Record<ViewerRole, string> = {
      anonymous: 'Request access',
      public_marketplace_visitor: 'Open listing',
      tenant_admin: 'Submit schema',
      platform_admin: 'Open approval',
      platform_support: 'Open DAG'
    };
    return ctas[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Schema Registry DAG': 'מאגר סכמות ומפת תלות',
      'Submission': 'הגשה',
      'Approval': 'אישור',
      'DAG': 'מפת תלות',
      'Role': 'תפקיד',
      'Schema versions': 'גרסאות סכמה',
      'Approval workflow': 'תהליך אישור',
      'Dependency graph': 'גרף תלות'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['submission', 'approval', 'dag'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
