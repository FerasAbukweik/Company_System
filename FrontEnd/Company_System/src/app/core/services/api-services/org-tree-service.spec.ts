import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { OrgTreeService } from './org-tree-service';
import { OrgTreeApiService } from '../api/org-tree-api-service';
import { ToastService } from './toast-service';
import { OrgNodeDTO } from '../../dto/organization-hierarchy/org-node';

describe('Organization tree loading', () => {
  const api = { GetChildren: vi.fn() };
  let service: OrgTreeService;

  beforeEach(() => {
    api.GetChildren.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: OrgTreeApiService, useValue: api },
        { provide: ToastService, useValue: { error: vi.fn() } },
      ],
    });
    service = TestBed.inject(OrgTreeService);
  });

  it('accepts an empty organization and permits loading again after reset', () => {
    api.GetChildren.mockReturnValue(of({ root: [] }));
    service.loadMoreTree().subscribe();
    service.loadMoreTree().subscribe();
    expect(service.treeRoot()).toBeNull();
    expect(service.isLoadingTree()).toBe(false);
    expect(api.GetChildren).toHaveBeenCalledTimes(1);
    service.resetTree();
    service.loadMoreTree().subscribe();
    expect(api.GetChildren).toHaveBeenCalledTimes(2);
  });

  it('stops requesting exhausted leaves', () => {
    const root = { id: 'root', children: [] } as unknown as OrgNodeDTO;
    api.GetChildren.mockReturnValueOnce(of({ root: [root] })).mockReturnValue(of({ root: [] }));
    service.loadMoreTree().subscribe();
    service.loadMoreTree().subscribe();
    service.loadMoreTree().subscribe();
    expect(api.GetChildren).toHaveBeenCalledTimes(2);
  });
});
