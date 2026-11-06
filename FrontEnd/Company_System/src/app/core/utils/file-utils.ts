export function validateImageSize(file: File, maxImageSize: number): 'ok' | 'too large' | 'wrong type' {
  if (!file.type.startsWith('image/') || file.size === 0) {
    return 'wrong type';
  }
  if (file.size > maxImageSize) {
    return 'too large';
  }
  return 'ok';
}
