import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { ToApproveDTO } from '../../dto/to-approve-dto';
import { RequestedApprovalDTO } from '../../dto/requested-approval-dto';
import { ApprovalStatusEnum } from '../../enums/approval-state-enum';
import { LazyDTO } from '../../dto/lazy-dto';

@Injectable({ providedIn: 'root' })
export class ApprovalApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly url = Urls.api + '/Approval';

  // api calls

  public getToApprove(lazyData: LazyDTO) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    return this.http.get<ToApproveDTO[]>(this.url + '/GetNeedsApproval', { params });
  }

  public getRequested(lazyData: LazyDTO) {
    let params = new HttpParams();

    Object.entries(lazyData).forEach(([key, val]) => {
      params = params.append(key, val);
    });

    return this.http.get<RequestedApprovalDTO[]>(this.url + '/GetRequested', { params });
  }

  public updateStatus(approvalId: string, newStatus: ApprovalStatusEnum) {
    let params = new HttpParams();

    params = params.append('newStatus', newStatus);

    return this.http.put(this.url + `/UpdateStatus/${approvalId}`, {}, { params });
  }

  public RequestHoliday() {
    return this.http.post(this.url + '/RequestHoliday', {});
  }
}
