import { PositionsEnum } from '../enum/positions-enum';

export interface OrgNodeDTO {
  id: string;
  position: PositionsEnum;
  userId: string;
  children: OrgNodeDTO[];
  userName: string;
  userImageUrl: string
}
