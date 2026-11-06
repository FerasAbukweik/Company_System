import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnDestroy,
  OnInit,
  output,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { EmployeeParentPickerService } from './employee-parent-picker.service';
import { DebounceDirective } from '../../../../../shared/directives/debounce.directive';
import { Subscription } from 'rxjs';
import { IsVisableDirective } from '../../../../../shared/directives/is-visable.directive';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { LoadingComponent } from '../../../../../shared/components/loading/loading.component';

/** Searchable, paginated picker for an employee’s manager. */
@Component({
  selector: 'app-employee-parent-picker',
  imports: [NgClass, IsVisableDirective, IconComponent, LoadingComponent, DebounceDirective],
  providers: [EmployeeParentPickerService],
  templateUrl: './employee-parent-picker.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
})
export class EmployeeParentPickerComponent implements OnInit, OnDestroy {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly picker = inject(EmployeeParentPickerService);
  private request?: Subscription;
  private readonly destroyRef = inject(DestroyRef);

  // inputs
  excludedUserId = input<string>('');
  selectedId = input<string>('');
  invalid = input<boolean>(false);

  // outputs
  parentSelected = output<string>();

  ngOnInit() {
    // start from a fresh list every time the picker is shown
    this.search('');
  }

  ngOnDestroy() {
    this.request?.unsubscribe();
  }

  protected search(value: string) {
    this.request?.unsubscribe();
    this.picker.reset(value);
    this.loadMore();
  }

  protected loadMore() {
    if (this.picker.loading() || this.picker.error()) return;
    this.request = this.picker
      .loadMore()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => {} });
  }
}
