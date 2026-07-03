import { RolesEnum } from '../../enums/role-enums';

export interface AuthDTO {
  isAuthenticated: boolean;
  roles: RolesEnum[];
  tokenExpiresAt: string;
}
