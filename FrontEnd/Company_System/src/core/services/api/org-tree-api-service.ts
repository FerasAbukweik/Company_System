import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { OrgNodeDTO } from '../../dto/org-node';
import { LazyDTO } from '../../dto/lazy-dto';
import { UserNameDTO } from '../../dto/username-dto';

@Injectable({ providedIn: 'root' })
export class OrgTreeApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private url = Urls.api + '/OrganizationHierarchy';

  // api calls

  public GetChildren(fatherIds: string[] | null) {
    let params = new HttpParams();

    if (fatherIds && fatherIds.length > 0) {
      fatherIds.forEach((id) => {
        params = params.append('parents', id);
      });
    }

    return this.http.get<Record<string, OrgNodeDTO[]>>(this.url, { params });
  }

  getUserNames(lazyData: LazyDTO) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    return this.http.get<UserNameDTO[]>(this.url + '/GetUserNames', { params });
  }
}
