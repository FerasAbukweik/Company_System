import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Urls } from '../../constants/urls';
import { LazyDTO } from '../../dto/lazy-loading/lazy-dto';
import { NotificationPageDTO } from '../../dto/notification/notification-page-dto';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = Urls.api + '/Notifications';

  get(paging: LazyDTO) {
    return this.http.get<NotificationPageDTO>(this.base, { params: { ...paging } });
  }

  unreadCount() {
    return this.http.get<number>(this.base + '/GetUnreadCount');
  }

  markAllRead() {
    return this.http.put<boolean>(this.base + '/MarkAllRead', {});
  }
}
