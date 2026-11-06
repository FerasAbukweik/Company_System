import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EmployeeParentPickerService } from './employee-parent-picker.service';

describe('Manager picker pagination', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [EmployeeParentPickerService, provideHttpClient(), provideHttpClientTesting()],
    }),
  );

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('searches on the server, pages matching employees, and reuses cached pages', () => {
    const service = TestBed.inject(EmployeeParentPickerService);
    const http = TestBed.inject(HttpTestingController);
    const response = (id: string) => ({ items: [{ id, fullName: id, treeId: id }], totalCount: 2 });

    service.reset(' Alice ');
    service.loadMore().subscribe();
    const first = http.expectOne((request) => request.params.get('search') === 'Alice');
    expect(first.request.params.get('taken')).toBe('0');
    expect(first.request.params.get('sectionSize')).toBe('10');
    expect(first.request.params.get('activeOnly')).toBe('true');
    first.flush(response('one'));

    service.loadMore().subscribe();
    http.expectOne((request) => request.params.get('taken') === '1').flush(response('two'));
    expect(service.employees().map((employee) => employee.id)).toEqual(['one', 'two']);
    service.loadMore().subscribe();
    http.expectNone(() => true);

    service.reset('Alice');
    service.loadMore().subscribe();
    http.expectNone(() => true);
    expect(service.employees().map((employee) => employee.id)).toEqual(['one']);

    service.reset('Bob');
    service.loadMore().subscribe();
    const filtered = http.expectOne((request) => request.params.get('search') === 'Bob');
    expect(filtered.request.params.get('taken')).toBe('0');
    filtered.flush({ items: [], totalCount: 0 });
    expect(service.employees()).toEqual([]);
  });
});
