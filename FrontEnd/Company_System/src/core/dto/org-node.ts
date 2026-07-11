import { PositionsEnum } from '../enum/positions-enum';

export interface OrgNodeDTO {
  id: string;
  position: PositionsEnum;
  userId: string;
  children: OrgNodeDTO[];
  isCurrUser: boolean;
  userName: string;
  userImageUrl: string
}
