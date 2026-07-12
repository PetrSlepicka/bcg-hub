export function formatPhone(phone?: string) {
  if (!phone) return "—";

  const digits = phone.replace(/\D/g, "");
  const nationalNumber = digits.length === 9 ? digits : digits.startsWith("00420") ? digits.slice(5) : digits.startsWith("420") ? digits.slice(3) : undefined;
  if (!nationalNumber || nationalNumber.length !== 9) return phone;

  return `+420 ${nationalNumber.slice(0, 3)} ${nationalNumber.slice(3, 6)} ${nationalNumber.slice(6)}`;
}
