import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Urls } from '../../constants/urls';
import { LazyDTO } from '../../dto/lazy-dto';
import { HttpParams } from '@angular/common/http';
import { MessageDTO } from '../../dto/message-dto';

@Injectable({ providedIn: 'root' })
export class MessagesApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly url = Urls.api + '/Messages';

  // api calls

  lazyGetMessages(lazyData: LazyDTO, otherUserId: string) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    params = params.append('otherUserId', otherUserId);

    return this.http.get<MessageDTO[]>(this.url, { params });
  }
}
