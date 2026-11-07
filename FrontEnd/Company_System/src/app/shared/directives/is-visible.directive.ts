import { Directive, ElementRef, inject, OnDestroy, OnInit, output } from '@angular/core';

/** Emits `visible` when the host element enters the screen (or its scroll container). */
@Directive({
  selector: '[appIsVisible]',
})
export class IsVisibleDirective implements OnInit, OnDestroy {
  private readonly host = inject(ElementRef);
  private observer!: IntersectionObserver;

  readonly visible = output<void>();

  ngOnInit() {
    this.observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) this.visible.emit();
    });

    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer.disconnect();
  }
}
