import { Directive, DestroyRef, ElementRef, inject, signal } from '@angular/core';

@Directive({
  selector: '[appDragPan]',
  host: {
    '[style.cursor]': "dragging() ? 'grabbing' : 'grab'",
    '[class.select-none]': 'dragging()',
    '(pointerdown)': 'start($event)',
    '(pointermove)': 'move($event)',
    '(pointerup)': 'end($event)',
    '(pointercancel)': 'end($event)',
    '(lostpointercapture)': 'end($event)',
    '(dragstart)': 'preventNativeDrag($event)',
  },
})
export class DragPanDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private pointerId: number | null = null;
  private startX = 0;
  private startY = 0;
  private scrollLeft = 0;
  private scrollTop = 0;

  protected readonly dragging = signal(false);

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stop());
  }

  protected start(event: PointerEvent) {
    if (event.pointerType !== 'mouse' || event.button !== 0 || this.pointerId !== null) return;
    if (
      event.target instanceof Element &&
      event.target.closest('button, a, input, select, textarea, [contenteditable], [role="button"]')
    )
      return;

    this.pointerId = event.pointerId;
    this.startX = event.clientX;
    this.startY = event.clientY;
    this.scrollLeft = this.host.scrollLeft;
    this.scrollTop = this.host.scrollTop;
    this.dragging.set(true);
    this.host.setPointerCapture(event.pointerId);
    event.preventDefault();
  }

  protected move(event: PointerEvent) {
    if (event.pointerId !== this.pointerId) return;
    this.host.scrollLeft = this.scrollLeft - (event.clientX - this.startX);
    this.host.scrollTop = this.scrollTop - (event.clientY - this.startY);
    event.preventDefault();
  }

  protected end(event: PointerEvent) {
    if (event.pointerId === this.pointerId) this.stop();
  }

  private stop() {
    const pointerId = this.pointerId;
    this.pointerId = null;
    this.dragging.set(false);
    if (pointerId !== null && this.host.hasPointerCapture(pointerId)) {
      this.host.releasePointerCapture(pointerId);
    }
  }

  protected preventNativeDrag(event: DragEvent) {
    if (this.pointerId !== null) event.preventDefault();
  }
}
