import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';

const VISIBLE_PAGE_BUTTONS = 3;

@Component({
  selector: 'app-employee-pagination',
  imports: [IconComponent],
  templateUrl: './employee-pagination.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'p-6 flex flex-col sm:flex-row items-center justify-between gap-4',
  },
})
export class EmployeePaginationComponent {
  protected readonly HeroIcon = HeroIcon;
  protected readonly pageSizeOptions = [5, 10, 25, 50];

  // inputs
  total = input.required<number>();
  label = input('employees');

  // two-way bound
  page = model.required<number>();
  pageSize = model.required<number>();

  // computed
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize())),
  );
  protected readonly firstItem = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly lastItem = computed(() =>
    Math.min(this.page() * this.pageSize(), this.total()),
  );

  protected readonly pageButtons = computed(() => {
    const totalPages = this.totalPages();
    const count = Math.min(VISIBLE_PAGE_BUTTONS, totalPages);
    const start = Math.min(Math.max(1, this.page() - 1), totalPages - count + 1);
    return Array.from({ length: count }, (_, index) => start + index);
  });

  // methods
  protected goTo(page: number) {
    this.page.set(Math.min(Math.max(1, page), this.totalPages()));
  }

  protected onPageSize(event: Event) {
    this.pageSize.set(Number((event.target as HTMLSelectElement).value));
    this.page.set(1);
  }
}
