import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { LazyDTO } from '../../dto/lazy-dto';
import { TaskDTO } from '../../dto/task-dto';
import { TaskStatusEnum } from '../../enum/task-states-enum';
import { TaskAddDTO } from '../../dto/task-add-dto';

@Injectable({ providedIn: 'root' })
export class TasksApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly url = Urls.api + '/Tasks';

  // methods

  // lazy get tasks
  public lazyGetTasks(lazyData: LazyDTO) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    return this.http.get<TaskDTO[]>(this.url, { params });
  }

  // update task status
  public updateStatus(taskId: string, newStatus: TaskStatusEnum) {
    let params = new HttpParams();
    params = params.append('newStatus', newStatus);

    return this.http.put(`${this.url}/UpdateStatus/${taskId}`, {}, { params });
  }

  public addTask(taskData: TaskAddDTO) {
    return this.http.post(this.url + '/Add', taskData);
  }
}
