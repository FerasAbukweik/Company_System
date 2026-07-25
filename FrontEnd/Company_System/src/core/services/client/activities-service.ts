import { inject, Injectable, signal } from '@angular/core';
import { ActivitiesApiService } from '../api/activities-api-service';
import { LazyDTO } from '../../dto/lazy-dto';
import { ActivityDTO } from '../../dto/activity-dto';
import { ToastService } from './toast-service';
import { Subject, takeUntil } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  // DI
  private readonly _activitiesApiService = inject(ActivitiesApiService);
  private readonly _toastService = inject(ToastService);

  // subject
  readonly cancelRequest$ = new Subject<void>();

  // private
  private _isMoreDataAvaiable = true;
  private _lazyData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };

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
    this._lazyData.taken = 0;
  }

  loadMore() {
    if (this._isActivitiesLoading() || !this._isMoreDataAvaiable) return;
    this._isActivitiesLoading.set(true);

    this._activitiesApiService
      .lazyGet(this._lazyData)
      .pipe(takeUntil(this.cancelRequest$))
      .subscribe({
        next: (data) => {
          this._activities.update((curr) => [...curr, ...data]);

          this._lazyData.taken += data.length;
          this._isMoreDataAvaiable = data.length > 0;
          this._isActivitiesLoading.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong while loading activities');
          this._isActivitiesLoading.set(false);
        },
      });
  }
}
