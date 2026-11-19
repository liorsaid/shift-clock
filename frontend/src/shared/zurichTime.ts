const zurichDateTime = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Zurich',
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

const monthAndYear = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric', timeZone: 'UTC' })

export function formatZurichTime(timeFromBackend: string): string {
  return zurichDateTime.format(new Date(timeFromBackend))
}

export function formatMonth(year: number, month: number): string {
  return monthAndYear.format(Date.UTC(year, month - 1))
}

export function formatDuration(totalSeconds: number): string {
  const totalMinutes = Math.floor(totalSeconds / 60)
  return `${Math.floor(totalMinutes / 60)} h ${totalMinutes % 60} min`
}
