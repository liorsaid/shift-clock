const zurichDateTime = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Zurich',
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export function formatZurichTime(timeFromBackend: string): string {
  return zurichDateTime.format(new Date(timeFromBackend))
}
