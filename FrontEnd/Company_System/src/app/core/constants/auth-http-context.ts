import { HttpContextToken } from '@angular/common/http';

export const SKIP_AUTH_RECOVERY = new HttpContextToken<boolean>(() => false);
