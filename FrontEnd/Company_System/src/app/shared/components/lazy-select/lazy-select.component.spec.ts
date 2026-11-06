import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LazySelectComponent } from './lazy-select.component';
import { LazySelectSource } from './lazy-select.source';

describe('LazySelectComponent', () => {
  let observed: IntersectionObserverCallback;

  beforeEach(() => {
    vi.stubGlobal(
      'IntersectionObserver',
      class {
        constructor(callback: IntersectionObserverCallback) {
          observed = callback;
        }
        observe() {}
        unobserve() {}
        disconnect() {}
      },
    );
  });

  afterEach(() => vi.unstubAllGlobals());

  it('loads on opening and scrolling, emits a selected value, and supports retry', async () => {
    const options = signal([{ value: 'one', name: 'First Employee' }]);
    const failed = signal(false);
    const source: LazySelectSource = {
      options,
      failed,
      loading: signal(false),
      hasMore: signal(true),
      open: vi.fn(),
      loadMore: vi.fn(),
      ensure: vi.fn(),
      retry: vi.fn(),
    };
    const fixture = TestBed.createComponent(LazySelectComponent);
    fixture.componentRef.setInput('source', source);
    const picked = vi.fn();
    fixture.componentInstance.value.subscribe(picked);
    await fixture.whenStable();

    const host = fixture.nativeElement as HTMLElement;
    const trigger = host.querySelector('button')!;
    expect(source.open).not.toHaveBeenCalled();
    trigger.click();
    await fixture.whenStable();
    expect(source.open).toHaveBeenCalledOnce();

    observed([{ isIntersecting: true } as IntersectionObserverEntry], {} as IntersectionObserver);
    expect(source.loadMore).toHaveBeenCalledOnce();
    options.set([...options(), { value: 'two', name: 'Second Employee' }]);
    await fixture.whenStable();
    (host.querySelectorAll('[role="option"]')[1] as HTMLElement).click();
    expect(picked).toHaveBeenCalledWith('two');
    await fixture.whenStable();
    expect(trigger.textContent).toContain('Second Employee');

    trigger.click();
    failed.set(true);
    await fixture.whenStable();
    const retry = [...host.querySelectorAll('button')].find((button) =>
      button.textContent?.includes('Retry'),
    )!;
    retry.click();
    expect(source.retry).toHaveBeenCalledOnce();
  });

  it('supports keyboard selection and disabled controls', async () => {
    const fixture = TestBed.createComponent(LazySelectComponent);
    fixture.componentRef.setInput('options', [{ value: 'one', name: 'Employee' }]);
    await fixture.whenStable();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    button.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(fixture.componentInstance.value()).toBe('one');

    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });
});
