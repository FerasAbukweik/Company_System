import { inject, Injectable, signal } from '@angular/core';
import { OrgTreeApiService } from '../api/org-tree-api-service';
import { OrgNodeDTO } from '../../dto/org-node';
import { Subject, takeUntil } from 'rxjs';
import { ToastService } from './toast-service';
import { UserNameDTO } from '../../dto/username-dto';
import { LazyDTO } from '../../dto/lazy-dto';

@Injectable({ providedIn: 'root' })
export class OrgTreeService {
  // DI
  private readonly _orgTreeApiService = inject(OrgTreeApiService);
  private _toastService = inject(ToastService);

  // signals
  private _orgTreeRoot = signal<OrgNodeDTO | null>(null);
  private _isLoadingTree = signal<boolean>(false);
  private _userNames = signal<UserNameDTO[]>([]);
  private _isLoadingUserNames = signal<boolean>(false);

  // sets
  private _nodesWithnoChildren = new Set<string>();

  // private
  private _lazyUserNamesData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
  private _areMoreUserNamesAvaiable = true;

  // subjects
  readonly cancelTreeRequests$ = new Subject<void>();
  readonly cancelUserNamesRequests$ = new Subject<void>();

  // getters

  get treeRoot() {
    return this._orgTreeRoot.asReadonly();
  }

  get isLoadingTree() {
    return this._isLoadingTree.asReadonly();
  }

  get userNames() {
    return this._userNames.asReadonly();
  }

  get isLoadingUserNames() {
    return this._isLoadingUserNames.asReadonly();
  }

  // methods

  reset() {
    this.resetTree();
    this.resetUserNames();
  }

  resetTree() {
    this.cancelTreeRequests$.next();

    this._orgTreeRoot.set(null);
    this._isLoadingTree.set(false);
    this._nodesWithnoChildren = new Set<string>();
  }

  resetUserNames() {
    this.cancelUserNamesRequests$.next();

    this._userNames.set([]);
    this._isLoadingUserNames.set(false);
    this._areMoreUserNamesAvaiable = true;
    this._lazyUserNamesData.taken = 0;
  }

  private mergeChildren(
    targetFathers: Set<string>,
    node: OrgNodeDTO,
    newData: Record<string, OrgNodeDTO[]>,
  ): OrgNodeDTO {
    if (targetFathers.has(node.id)) {
      return {
        ...node,
        children: newData[node.id],
      };
    }

    return {
      ...node,
      children: node.children.map((c) => this.mergeChildren(targetFathers, c, newData)),
    };
  }

  // load more tree children
  private loadChildren(fatherIds: string[] | null) {
    if (this._isLoadingTree()) return;
    this._isLoadingTree.set(true);

    const fatherIdsSet = new Set(fatherIds || []);

    this._orgTreeApiService
      .GetChildren(fatherIds)
      .pipe(takeUntil(this.cancelTreeRequests$))
      .subscribe({
        next: (data) => {
          // add data to memory

          if (fatherIdsSet.size == 0 || !this._orgTreeRoot) {
            // should return one node which is the root

            const result = Object.values(data);
            if (result.length == 0 || result[0].length == 0) {
              this._toastService.error('server returned bad tree data');
            }

            this._orgTreeRoot.set(Object.values(data)[0][0]);
          } else {
            this._orgTreeRoot.update((curr) => this.mergeChildren(new Set(fatherIds), curr!, data));
          }

          // update status

          // add fathers with no children to this.nodesWithnoChildren
          Object.entries(data).forEach(([key, val]) => {
            if (val.length == 0) this._nodesWithnoChildren.add(key);
          });
          this._isLoadingTree.set(false);
        },
        error: () => {
          this._toastService.error('failed fetching org tree');
          this._isLoadingTree.set(false);
        },
      });
  }

  private leafeIds: string[] = [];
  private updateLeaves(node: OrgNodeDTO) {
    if (node.children.length == 0) this.leafeIds.push(node.id);

    node.children.forEach((c) => this.updateLeaves(c));
  }

  // load more Tree
  loadMoreTree() {
    if (!this._orgTreeRoot()) {
      this.loadChildren(null);
      return;
    }

    this.leafeIds = [];
    this.updateLeaves(this._orgTreeRoot()!);

    this.loadChildren(this.leafeIds);
  }

  // load more usernames
  loadMoreUserNames() {
    if (this._isLoadingUserNames() || !this._areMoreUserNamesAvaiable) return;
    this._isLoadingUserNames.set(true);

    this._orgTreeApiService
      .getUserNames(this._lazyUserNamesData)
      .pipe(takeUntil(this.cancelUserNamesRequests$))
      .subscribe({
        next: (data) => {
          this._userNames.update((curr) => [...curr, ...data]);

          this._areMoreUserNamesAvaiable = data.length > 0;
          this._lazyUserNamesData.taken += data.length;
          this._isLoadingUserNames.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong fetching usernames');

          this._isLoadingUserNames.set(false);
        },
      });
  }
}
