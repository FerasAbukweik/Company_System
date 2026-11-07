import { workedHours } from './attendance-time';

describe('Attendance elapsed hours', () => {
  it('requires both times and includes overnight work', () => {
    expect(workedHours(null, '16:00:00')).toBeNull();
    expect(workedHours('08:00:00', null)).toBeNull();
    expect(workedHours('08:00:00', '15:30:00')).toBe(7.5);
    expect(workedHours('22:00:00', '06:00:00')).toBe(8);
  });
});
