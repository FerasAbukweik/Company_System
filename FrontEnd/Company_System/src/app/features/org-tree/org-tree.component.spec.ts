import { TestBed } from '@angular/core/testing';
import { EMPTY } from 'rxjs';
import { OrgTreeComponent } from './org-tree.component';
import { OrgTreeService } from '../../core/services/api-services/org-tree-service';
import { ChatPanelService } from './component/chat-panel/chat-panel.service';

describe('Org tree viewport centering', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('centers wide content and preserves the drag offset as branches load', async () => {
    let resize!: ResizeObserverCallback;
    const disconnect = vi.fn();
    vi.stubGlobal(
      'ResizeObserver',
      class {
        constructor(callback: ResizeObserverCallback) {
          resize = callback;
        }
        observe() {}
        disconnect = disconnect;
      },
    );
    TestBed.configureTestingModule({
      providers: [
        {
          provide: OrgTreeService,
          useValue: { loadMoreTree: () => EMPTY, cancelTreeRequests$: { next: vi.fn() } },
        },
        { provide: ChatPanelService, useValue: {} },
      ],
    }).overrideComponent(OrgTreeComponent, {
      set: {
        imports: [],
        template: '<div #treeViewport><div #treeContent></div></div>',
      },
    });
    const fixture = TestBed.createComponent(OrgTreeComponent);
    await fixture.whenStable();
    const viewport = fixture.nativeElement.firstElementChild as HTMLElement;
    let width = 1200;
    Object.defineProperties(viewport, {
      scrollWidth: { get: () => width },
      clientWidth: { value: 400 },
    });
    resize([], {} as ResizeObserver);
    expect(viewport.scrollLeft).toBe(400);
    viewport.scrollLeft = 100;
    width = 1600;
    resize([], {} as ResizeObserver);
    expect(viewport.scrollLeft).toBe(300);
    resize([], {} as ResizeObserver);
    expect(viewport.scrollLeft).toBe(300);
    fixture.destroy();
    expect(disconnect).toHaveBeenCalledOnce();
  });
});
