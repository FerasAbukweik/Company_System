export function toTimeInput(value: string | null): string {
  return value?.slice(0, 5) ?? '';
}

export function toTimeValue(value: string): string | null {
  return value ? `${value}:00` : null;
}

export function currentTime(): string {
  const now = new Date();
  return `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
}

export function workedHours(clockIn: string | null, clockOut: string | null): number | null {
  if (!clockIn || !clockOut) return null;

  const seconds = (time: string) => {
    const [hours, minutes, seconds = 0] = time.split(':').map(Number);
    return hours * 3600 + minutes * 60 + seconds;
  };
  const duration = seconds(clockOut) - seconds(clockIn);
  if (!Number.isFinite(duration)) return null;

  return (duration < 0 ? duration + 86_400 : duration) / 3600;
}
