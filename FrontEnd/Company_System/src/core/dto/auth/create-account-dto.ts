import { RolesEnum } from "../../enums/role-enums";  // تأكد من المسار

export interface AccountCreateDTO {
  email: string;
  password: string;
  userName: string;
  fullName: string;
  phoneNumber: string;
  role: RolesEnum;
}