import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Urls } from '../../constants/urls';
import { LazyDTO } from '../../dto/lazy-dto';
import { TaskDTO } from '../../dto/task-dto';

@Injectable({ providedIn: 'root' })
export class TasksApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly url = Urls.api + '/Tasks';

  public lazyGetTasks(lazyData: LazyDTO) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    return this.http.get<TaskDTO[]>(this.url, { params });
  }
}
