export function customerDisplayName(name) {
  return name?.replace(/^\[DEMO\]\s*/i, '')
}
