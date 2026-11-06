export function apiError(error: unknown): string {
  const value = error as {
    error?: { message?: string; title?: string; errors?: Record<string, string[]> } | string;
  };
  if (typeof value?.error === 'string') return value.error;
  if (value?.error?.errors) return Object.values(value.error.errors).flat().join(' ');
  return (
    value?.error?.message ||
    value?.error?.title ||
    'Unable to complete the request. Please try again.'
  );
}
