/** A display-ready summary card. Feature components own the calculations behind each value. */
export interface StatusData {
  label: string;
  value: string | number;
  format?: 'currency';
  currency?: string;
  description?: string;
  detail?: string;
  detailDivider?: boolean;
  icon?: string;
  color?: string;
}
