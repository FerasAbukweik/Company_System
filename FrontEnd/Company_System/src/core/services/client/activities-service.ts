import { inject, Injectable, signal } from '@angular/core';
import { ActivitiesApiService } from '../api/activities-api-service';
import { Urls } from '../../constants/urls';
import { LazyDTO } from '../../dto/lazy-dto';
import { ActivityDTO } from '../../dto/activity-dto';
import { ToastService } from './toast-service';
import { Subject, takeUntil } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  // DI
  private readonly activitiesApiService = inject(ActivitiesApiService);
  private readonly toastService = inject(ToastService);

  // subject
  private cancelRequest$ = new Subject<void>();

  // private
  private readonly url = Urls.api + '/Activities';
  private isMoreDataAvaiable = true;
  private lazyData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };

  // signals
  private activities = signal<ActivityDTO[]>([]);
  private isActivitiesLoading = signal<boolean>(false);

  // getters
  get getActivities() {
    return this.activities.asReadonly();
  }

  get getIsActivitiesLoading() {
    return this.isActivitiesLoading.asReadonly();
  }

  // methods

  reset() {
    this.cancelRequest$.next();

    this.activities.set([]);
    this.isActivitiesLoading.set(false);
    this.isMoreDataAvaiable = true;
    this.lazyData.taken = 0;
  }

  loadMore() {
    if (this.isActivitiesLoading() || !this.isMoreDataAvaiable) return;
    this.isActivitiesLoading.set(true);

    this.activitiesApiService
      .lazyGet(this.lazyData)
      .pipe(takeUntil(this.cancelRequest$))
      .subscribe({
        next: (data) => {
          this.activities.update((curr) => [...curr, ...data]);

          this.lazyData.taken += data.length;
          this.isMoreDataAvaiable = data.length > 0;
          this.isActivitiesLoading.set(false);
        },
        error: () => {
          this.toastService.error('something went wrong while loading activities');
          this.isActivitiesLoading.set(false);
        },
      });
  }
}
