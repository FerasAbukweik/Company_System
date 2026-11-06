import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DragPanDirective } from './drag-pan.directive';

@Component({
  imports: [DragPanDirective],
  template: '<div appDragPan><span>Tree node</span><button>Open chat</button></div>',
})
class PanHost {}

function pointer(type: string, x = 100, y = 100, pointerType = 'mouse', button = 0) {
  const event = new MouseEvent(type, {
    bubbles: true,
    cancelable: true,
    clientX: x,
    clientY: y,
    button,
  });
  Object.defineProperties(event, { pointerId: { value: 1 }, pointerType: { value: pointerType } });
  return event;
}

const setup = () => {
  const fixture = TestBed.createComponent(PanHost);
  fixture.detectChanges();
  const viewport = fixture.nativeElement.querySelector('div') as HTMLDivElement;
  Object.assign(viewport, {
    setPointerCapture: vi.fn(),
    hasPointerCapture: () => true,
    releasePointerCapture: vi.fn(),
  });
  return { fixture, viewport };
};

describe('DragPanDirective', () => {
  it('pans both axes with the mouse and restores the open-hand cursor on release', () => {
    const { fixture, viewport } = setup();
    viewport.scrollLeft = 100;
    viewport.scrollTop = 200;
    expect(viewport.style.cursor).toBe('grab');
    viewport.querySelector('span')!.dispatchEvent(pointer('pointerdown'));
    fixture.detectChanges();
    expect(viewport.style.cursor).toBe('grabbing');
    expect(viewport.setPointerCapture).toHaveBeenCalledWith(1);
    viewport.dispatchEvent(pointer('pointermove', 130, 120));
    expect(viewport.scrollLeft).toBe(70);
    expect(viewport.scrollTop).toBe(180);
    viewport.dispatchEvent(pointer('pointerup', 130, 120));
    fixture.detectChanges();
    expect(viewport.style.cursor).toBe('grab');
    expect(viewport.releasePointerCapture).toHaveBeenCalledWith(1);
    viewport.dispatchEvent(pointer('pointermove', 150, 150));
    expect(viewport.scrollLeft).toBe(70);
  });

  it('preserves buttons, touch scrolling, and secondary mouse clicks', () => {
    const { viewport } = setup();
    viewport.querySelector('button')!.dispatchEvent(pointer('pointerdown'));
    viewport.dispatchEvent(pointer('pointerdown', 100, 100, 'touch'));
    viewport.dispatchEvent(pointer('pointerdown', 100, 100, 'mouse', 2));
    expect(viewport.setPointerCapture).not.toHaveBeenCalled();
  });

  it('ends panning when the pointer is cancelled', () => {
    const { fixture, viewport } = setup();
    viewport.dispatchEvent(pointer('pointerdown'));
    viewport.dispatchEvent(pointer('pointercancel'));
    fixture.detectChanges();
    expect(viewport.style.cursor).toBe('grab');
    expect(viewport.classList.contains('select-none')).toBe(false);
  });
});
