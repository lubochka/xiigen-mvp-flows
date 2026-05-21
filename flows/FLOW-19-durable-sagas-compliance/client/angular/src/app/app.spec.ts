import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-19 durable sagas compliance UI', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App]
    }).compileComponents();
  });

  it('renders the operation progress strip without raw state-machine names', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('operations');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(fixture.nativeElement.querySelector('[data-testid="saga-operations-page"]')).toBeTruthy();
    expect(text).toContain('Usually takes 2 business days');
    expect(text).not.toContain('COMPENSATING_STAGE');
    expect(text).not.toContain('DataProcessResult');
  });

  it('shows retry and rollback controls only to operating roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('sagas');
    fixture.componentInstance.setRole('tenant_ops');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Retry failed step');

    fixture.componentInstance.setRole('tenant_consumer');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Retry failed step');
  });

  it('renders compliance audit export controls for platform roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('compliance');
    fixture.componentInstance.setRole('compliance_officer');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="compliance-audit-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Export CSV');
  });

  it('sets html language and direction for Hebrew evidence', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
