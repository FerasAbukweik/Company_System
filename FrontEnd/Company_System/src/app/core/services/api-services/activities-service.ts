import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { inject, Injectable, signal } from '@angular/core';
import { ActivitiesApiService } from '../api/activities-api-service';
import { ActivityDTO } from '../../dto/activity/activity-dto';
import { EMPTY, Subject, defer, finalize, takeUntil, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _activitiesApiService = inject(ActivitiesApiService);

  // subject
  readonly cancelRequest$ = new Subject<void>();

  // private
  readonly sectionSize = 10;
  private _isMoreDataAvaiable = true;

  // signals
  private _activities = signal<ActivityDTO[]>([]);
  private _isActivitiesLoading = signal<boolean>(false);

  // getters
  get activities() {
    return this._activities.asReadonly();
  }

  get isActivitiesLoading() {
    return this._isActivitiesLoading.asReadonly();
  }

  // methods

  reset() {
    this.cancelRequest$.next();

    this._activities.set([]);
    this._isActivitiesLoading.set(false);
    this._isMoreDataAvaiable = true;
  }

  loadMore() {
    return defer(() => {
      if (this._isActivitiesLoading() || !this._isMoreDataAvaiable) return EMPTY;
      this._isActivitiesLoading.set(true);

      return this._activitiesApiService
        .lazyGet({ taken: this._activities().length, sectionSize: this.sectionSize })
        .pipe(toastError(this._toastService, 'تعذر تحميل الأنشطة'), takeUntil(this.cancelRequest$))
        .pipe(
          tap({
            next: (data) => {
              this._activities.update((curr) => [...curr, ...data]);

              this._isMoreDataAvaiable = data.length > 0;
            },
          }),
          finalize(() => {
            this._isActivitiesLoading.set(false);
          }),
        );
    });
  }
}
