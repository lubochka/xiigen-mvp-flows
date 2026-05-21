import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'referral_user'
  | 'flow_author'
  | 'prompt_approver'
  | 'human_decision_maker'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'canvas' | 'publisher' | 'debug';
type Mode = 'Design' | 'Preview' | 'Active' | 'Debug';

interface FlowNode {
  id: string;
  label: string;
  category: 'trigger' | 'action' | 'gate' | 'transform' | 'output';
  task: string;
  status: 'ready' | 'warning' | 'blocked';
  x: number;
  y: number;
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
    'public_visitor',
    'tenant_user',
    'tenant_admin',
    'referral_user',
    'flow_author',
    'prompt_approver',
    'human_decision_maker',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('flow_author');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('canvas');
  readonly moduleName = signal('Visual Flow Engine');
  readonly selectedNode = signal('n2');
  readonly mode = signal<Mode>('Design');
  readonly runName = signal('Happy path checkout');

  readonly nodes: FlowNode[] = [
    { id: 'n1', label: 'Request intake', category: 'trigger', task: 'Canvas writer', status: 'ready', x: 8, y: 24 },
    { id: 'n2', label: 'Decision gate', category: 'gate', task: 'Publication check', status: 'warning', x: 36, y: 18 },
    { id: 'n3', label: 'Node catalog', category: 'action', task: 'Type registration', status: 'ready', x: 62, y: 38 },
    { id: 'n4', label: 'Code handoff', category: 'output', task: 'Injection audit', status: 'ready', x: 82, y: 25 }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'עורך זרימות חזותי' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-visual-flow-engine`);
  readonly selected = computed(() => this.nodes.find(node => node.id === this.selectedNode()) ?? this.nodes[0]);
  readonly canDesign = computed(() => ['tenant_admin', 'flow_author', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly canPublish = computed(() => ['tenant_admin', 'flow_author', 'prompt_approver', 'platform_admin'].includes(this.role()));
  readonly canOperate = computed(() => ['flow_author', 'human_decision_maker', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly issueCount = computed(() => this.nodes.filter(node => node.status !== 'ready').length);

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
    this.mode.set(value === 'debug' ? 'Debug' : value === 'publisher' ? 'Preview' : 'Design');
  }

  setSelectedNode(value: string): void {
    this.selectedNode.set(value);
  }

  setRunName(value: string): void {
    this.runName.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest viewer',
      public_visitor: 'Public preview',
      tenant_user: 'Workspace member',
      tenant_admin: 'Workspace admin',
      referral_user: 'Referral viewer',
      flow_author: 'Flow author',
      prompt_approver: 'Prompt approver',
      human_decision_maker: 'Decision reviewer',
      platform_admin: 'Platform admin',
      platform_support: 'Support specialist'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview the published shape without tenant-private inputs',
      public_visitor: 'Inspect a safe demonstration before signing in',
      tenant_user: 'Run approved flows and inspect readable outcomes',
      tenant_admin: 'Own workspace drafts, publish readiness, and access',
      referral_user: 'Review shared automation status without edit access',
      flow_author: 'Compose nodes, inspect edges, and prepare deployments',
      prompt_approver: 'Approve generated prompts before publication',
      human_decision_maker: 'Review blocked paths and manual decisions',
      platform_admin: 'Govern node catalogs, deployments, and tenant bounds',
      platform_support: 'Repair stuck versions and investigate failed runs'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Visual Flow Engine': 'עורך זרימות חזותי',
      'Role': 'תפקיד',
      'Canvas': 'קנבס',
      'Publish': 'פרסום',
      'Debug': 'בדיקה',
      'Selected node': 'צומת נבחר',
      'Run name': 'שם הרצה',
      'Deploy': 'פריסה',
      'Run test': 'הרצת בדיקה',
      'Open actions': 'פעולות זמינות',
      'Preview only': 'תצוגה בלבד'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['canvas', 'publisher', 'debug'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
