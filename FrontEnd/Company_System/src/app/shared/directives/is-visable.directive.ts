import {
  Directive,
  effect,
  ElementRef,
  inject,
  input,
  OnDestroy,
  output,
  untracked,
} from '@angular/core';

@Directive({
  selector: '[appIsVisable]',
})
export class IsVisableDirective implements OnDestroy {
  // DI
  private readonly host = inject(ElementRef);

  // private
  private observer?: IntersectionObserver;

  readonly recheck = input<unknown>(undefined, { alias: 'visibleRecheck' });

  constructor() {
    effect(() => {
      this.recheck();
      untracked(() => {
        if (!this.observer) return;
        this.observer.unobserve(this.host.nativeElement);
        this.observer.observe(this.host.nativeElement);
      });
    });
  }

  // output
  visible = output<void>();

  ngOnInit() {
    this.observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) this.visible.emit();
    });

    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
