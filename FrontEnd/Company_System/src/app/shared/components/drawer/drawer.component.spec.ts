import { TestBed } from '@angular/core/testing';
import { DrawerComponent } from './drawer.component';

describe('Drawer outside interactions', () => {
  it('starts closed with the panel and backdrop explicitly hidden', async () => {
    const fixture = TestBed.createComponent(DrawerComponent);
    fixture.componentRef.setInput('open', false);
    await fixture.whenStable();
    const panel: HTMLElement = fixture.nativeElement.querySelector('aside');
    const backdrop: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(panel.style.visibility).toBe('hidden');
    expect(backdrop.style.visibility).toBe('hidden');
    expect(panel.hasAttribute('inert')).toBe(true);
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    expect(panel.style.visibility).toBe('visible');
    expect(backdrop.style.visibility).toBe('visible');
    fixture.componentRef.setInput('open', false);
    await fixture.whenStable();
    expect(panel.style.visibility).toBe('hidden');
  });

  it('ignores a trailing click from opening and closes on a fresh backdrop press', async () => {
    const fixture = TestBed.createComponent(DrawerComponent);
    fixture.componentRef.setInput('open', true);
    const closed = vi.fn();
    fixture.componentInstance.closed.subscribe(closed);
    await fixture.whenStable();

    const backdrop: HTMLElement = fixture.nativeElement.querySelector('div');
    backdrop.click();
    expect(closed).not.toHaveBeenCalled();
    fixture.nativeElement
      .querySelector('aside')
      .dispatchEvent(new Event('pointerdown', { bubbles: true }));
    expect(closed).not.toHaveBeenCalled();
    backdrop.dispatchEvent(new Event('pointerdown', { bubbles: true }));
    expect(closed).toHaveBeenCalledOnce();
  });
});
