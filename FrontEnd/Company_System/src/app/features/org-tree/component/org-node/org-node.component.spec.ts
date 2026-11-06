import { TestBed } from '@angular/core/testing';
import { OrgNodeComponent } from './org-node.component';
import { AuthService } from '../../../../core/services/api-services/auth-service';
import { DelegateTaskService } from '../delegate-task/delegate-task.service';
import { ChatPanelService } from '../chat-panel/chat-panel.service';
import { OrgNodeDTO } from '../../../../core/dto/organization-hierarchy/org-node';
import { PositionsEnum } from '../../../../core/enum/positions-enum';

const node = (id: string, children: OrgNodeDTO[] = []): OrgNodeDTO => ({
  id,
  userId: id,
  fullName: `Employee ${id}`,
  userName: id,
  position: PositionsEnum.Employee,
  userImageUrl: 'employee.png',
  children,
});

describe('Org tree branch layout', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { userData: () => ({ userId: 'root' }) } },
        { provide: DelegateTaskService, useValue: { open: vi.fn() } },
        { provide: ChatPanelService, useValue: { open: vi.fn() } },
      ],
    }),
  );

  it('gives each child one branch connector and keeps all cards the same minimum height', async () => {
    const fixture = TestBed.createComponent(OrgNodeComponent);
    fixture.componentRef.setInput(
      'node',
      node('root', [node('wide', [node('leaf')]), node('other')]),
    );
    fixture.componentRef.setInput('isCurrUserChild', false);
    await fixture.whenStable();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('.org-tree-branch')).toHaveLength(3);
    expect(host.querySelectorAll('.org-tree-connector')).toHaveLength(3);
    expect(host.querySelectorAll('.min-h-32')).toHaveLength(4);
    expect(host.textContent).toContain('Employee leaf');
    expect(host.querySelector('[class*="12.5%"]')).toBeNull();
  });

  it('does not draw branch connectors for a leaf node', async () => {
    const fixture = TestBed.createComponent(OrgNodeComponent);
    fixture.componentRef.setInput('node', node('leaf'));
    fixture.componentRef.setInput('isCurrUserChild', false);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.org-tree-children')).toBeNull();
  });
});
