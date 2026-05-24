import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Self-Developing Meta-Flow Engine App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the self-build workspace without leaking rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Self-build console');
    expect(text).toContain('Self-build starts only after existing capabilities are checked');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows genesis review posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('genesis');
    component.setRole('platform_support');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="genesis-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Generated artifacts must survive DNA and sandbox gates');
  });

  it('keeps anonymous users on public preview copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Public visitors see only the self-build posture');
  });

  it('renders promotion evidence for the approval gate', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('promotion');
    component.setRole('human_decision_maker');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="promotion-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('CORE promotion requires evidence and human approval');
    expect(fixture.nativeElement.textContent).toContain('no automated bypass');
  });

  it('keeps promotion authority restricted to human decision roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('human_decision_maker');
    component.setReadinessScore(99);
    fixture.detectChanges();

    expect(component.canApprove()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('99');
  });
});

