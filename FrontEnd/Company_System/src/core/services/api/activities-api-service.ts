import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Urls } from "../../constants/urls";
import { LazyDTO } from "../../dto/lazy-dto";
import { ActivityDTO } from "../../dto/activity-dto";

@Injectable({providedIn: 'root'})
export class ActivitiesApiService{
    // DI
    private readonly http = inject(HttpClient);

    // private
    private url = Urls.api + '/Activities'


    // methods

    public lazyGet(lazyData: LazyDTO){
        let params = new HttpParams();

        Object.entries(lazyData).forEach(([key, val]) => {
            params = params.append(key, val);
        })

        return this.http.get<ActivityDTO[]>(this.url, {params});
    }
}