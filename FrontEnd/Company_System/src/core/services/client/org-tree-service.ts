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
  private readonly orgTreeApiService = inject(OrgTreeApiService);
  private toastService = inject(ToastService);

  // signals
  private orgTreeRoot = signal<OrgNodeDTO | null>(null);
  private isLoadingTree = signal<boolean>(false);
  private userNames = signal<UserNameDTO[]>([]);
  private isLoadingUserNames = signal<boolean>(false);

  // sets
  private nodesWithnoChildren = new Set<string>();

  // private
  private lazyUserNamesData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
  private areMoreUserNamesAvaiable = true;

  // subjects
  private cancelTreeRequests$ = new Subject<void>();
  private cancelUserNamesRequests$ = new Subject<void>();

  // getters

  get getTreeRoot() {
    return this.orgTreeRoot.asReadonly();
  }

  get getIsLoadingTree() {
    return this.isLoadingTree.asReadonly();
  }

  get getUserNames() {
    return this.userNames.asReadonly();
  }

  get getIsLoadingUserNames() {
    return this.isLoadingUserNames.asReadonly();
  }

  // methods

  reset() {
    this.resetTree();
    this.resetUserNames();
  }

  resetTree() {
    this.cancelTreeRequests$.next();

    this.orgTreeRoot.set(null);
    this.isLoadingTree.set(false);
    this.nodesWithnoChildren = new Set<string>();
  }

  resetUserNames() {
    this.cancelUserNamesRequests$.next();

    this.userNames.set([]);
    this.isLoadingUserNames.set(false);
    this.areMoreUserNamesAvaiable = true;
    this.lazyUserNamesData.taken = 0;
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
    if (this.isLoadingTree()) return;
    this.isLoadingTree.set(true);

    const fatherIdsSet = new Set(fatherIds || []);

    this.orgTreeApiService
      .GetChildren(fatherIds)
      .pipe(takeUntil(this.cancelTreeRequests$))
      .subscribe({
        next: (data) => {
          // add data to memory

          if (fatherIdsSet.size == 0 || !this.orgTreeRoot) {
            // should return one node which is the root

            const result = Object.values(data);
            if (result.length == 0 || result[0].length == 0) {
              this.toastService.error('server returned bad tree data');
            }

            this.orgTreeRoot.set(Object.values(data)[0][0]);
          } else {
            this.orgTreeRoot.update((curr) => this.mergeChildren(new Set(fatherIds), curr!, data));
          }

          // update status

          // add fathers with no children to this.nodesWithnoChildren
          Object.entries(data).forEach(([key, val]) => {
            if (val.length == 0) this.nodesWithnoChildren.add(key);
          });
          this.isLoadingTree.set(false);
        },
        error: () => {
          this.toastService.error('failed fetching org tree');
          this.isLoadingTree.set(false);
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
    if (!this.orgTreeRoot()) {
      this.loadChildren(null);
      return;
    }

    this.leafeIds = [];
    this.updateLeaves(this.orgTreeRoot()!);

    this.loadChildren(this.leafeIds);
  }

  // load more usernames
  loadMoreUserNames() {
    if (this.isLoadingUserNames() || !this.areMoreUserNamesAvaiable) return;
    this.isLoadingUserNames.set(true);

    this.orgTreeApiService
      .getUserNames(this.lazyUserNamesData)
      .pipe(takeUntil(this.cancelUserNamesRequests$))
      .subscribe({
        next: (data) => {
          this.userNames.update((curr) => [...curr, ...data]);

          this.areMoreUserNamesAvaiable = data.length > 0;
          this.lazyUserNamesData.taken += data.length;
          this.isLoadingUserNames.set(false);
        },
        error: () => {
          this.toastService.error('something went wrong fetching usernames');

          this.isLoadingUserNames.set(false);
        },
      });
  }
}
