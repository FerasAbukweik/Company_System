import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { inject, Injectable, signal } from '@angular/core';
import { OrgTreeApiService } from '../api/org-tree-api-service';
import { OrgNodeDTO } from '../../dto/organization-hierarchy/org-node';
import { EMPTY, Subject, defer, finalize, takeUntil, tap } from 'rxjs';
import { UserNameDTO } from '../../dto/organization-hierarchy/username-dto';

@Injectable({ providedIn: 'root' })
export class OrgTreeService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _orgTreeApiService = inject(OrgTreeApiService);

  // signals
  private _orgTreeRoot = signal<OrgNodeDTO | null>(null);
  private _isLoadingTree = signal<boolean>(false);
  private _userNames = signal<UserNameDTO[]>([]);
  private _isLoadingUserNames = signal<boolean>(false);

  // sets
  private _nodesWithnoChildren = new Set<string>();

  // private
  readonly sectionSize = 10;
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
  }

  private mergeChildren(
    targetFathers: Set<string>,
    node: OrgNodeDTO,
    newData: Record<string, OrgNodeDTO[]>,
  ): OrgNodeDTO {
    if (targetFathers.has(node.id)) {
      return {
        ...node,
        children: newData[node.id] ?? node.children,
      };
    }

    return {
      ...node,
      children: node.children.map((c) => this.mergeChildren(targetFathers, c, newData)),
    };
  }

  // load more tree children
  private loadChildren(fatherIds: string[] | null) {
    return defer(() => {
      if (this._isLoadingTree()) return EMPTY;
      this._isLoadingTree.set(true);

      const fatherIdsSet = new Set(fatherIds || []);

      return this._orgTreeApiService
        .GetChildren(fatherIds)
        .pipe(
          toastError(this._toastService, 'تعذر تحميل الهيكل التنظيمي'),
          takeUntil(this.cancelTreeRequests$),
        )
        .pipe(
          tap({
            next: (data) => {
              // add data to memory

              if (fatherIdsSet.size == 0 || !this._orgTreeRoot()) {
                // should return one node which is the root

                const result = Object.values(data);
                if (result.length == 0 || result[0].length == 0) {
                  this._orgTreeRoot.set(null);
                  this._nodesWithnoChildren.add('root');
                  return;
                }

                this._orgTreeRoot.set(Object.values(data)[0][0]);
              } else {
                this._orgTreeRoot.update((curr) =>
                  this.mergeChildren(new Set(fatherIds), curr!, data),
                );
              }

              // update status

              // add fathers with no children to this.nodesWithnoChildren
              Object.entries(data).forEach(([key, val]) => {
                if (val.length == 0) this._nodesWithnoChildren.add(key);
              });
            },
          }),
          finalize(() => {
            this._isLoadingTree.set(false);
          }),
        );
    });
  }

  private leafeIds: string[] = [];
  private updateLeaves() {
    const root = this._orgTreeRoot();
    if (root == null) return;

    this.leafeIds = [];
    this.updateLeavesRecustion(root);
  }
  private updateLeavesRecustion(node: OrgNodeDTO) {
    if (node.children.length == 0) this.leafeIds.push(node.id);

    node.children.forEach((c) => this.updateLeavesRecustion(c));
  }

  // load more Tree
  loadMoreTree() {
    return defer(() => {
      if (!this._orgTreeRoot()) {
        return this._nodesWithnoChildren.has('root') ? EMPTY : this.loadChildren(null);
      }

      this.updateLeaves();

      // remove leafs we already know dont have children
      const leafIds = this.leafeIds.filter((id) => !this._nodesWithnoChildren.has(id));
      return leafIds.length ? this.loadChildren(leafIds) : EMPTY;
    });
  }

  // load more usernames
  loadMoreUserNames() {
    return defer(() => {
      if (this._isLoadingUserNames() || !this._areMoreUserNamesAvaiable) return EMPTY;
      this._isLoadingUserNames.set(true);

      return this._orgTreeApiService
        .getUserNames({ taken: this._userNames().length, sectionSize: this.sectionSize })
        .pipe(
          toastError(this._toastService, 'تعذر تحميل أسماء المستخدمين'),
          takeUntil(this.cancelUserNamesRequests$),
        )
        .pipe(
          tap({
            next: (data) => {
              this._userNames.update((curr) => [...curr, ...data]);

              this._areMoreUserNamesAvaiable = data.length > 0;
            },
          }),
          finalize(() => {
            this._isLoadingUserNames.set(false);
          }),
        );
    });
  }
}
