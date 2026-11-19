export type Employee = { employeeNumber: string; displayName: string }
export type Status = { onShiftSince: string | null }
export type ClockResult = { clockInAt: string; clockOutAt?: string }
export type MonthTotal = { year: number; month: number; totalSeconds: number }
export type Shift = { clockInAt: string; clockOutAt: string | null; isMissingClockOut: boolean }
export type ApiResponse<T> = { status: number; data?: T }

export const StatusCode = {
  NoConnection: 0,
  Ok: 200,
  NoContent: 204,
  Unauthorized: 401,
  Forbidden: 403,
  Conflict: 409,
  ServiceUnavailable: 503,
} as const

async function send<T>(method: 'GET' | 'POST', url: string, body?: unknown): Promise<ApiResponse<T>> {
  try {
    const response = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    const data = response.status === StatusCode.Ok ? ((await response.json()) as T) : undefined
    return { status: response.status, data }
  } catch {
    return { status: StatusCode.NoConnection }
  }
}

export const getMe = () => send<Employee>('GET', '/api/me')
export const signIn = (employeeNumber: string, pin: string) => send<Employee>('POST', '/api/sign-in', { employeeNumber, pin })
export const signOut = () => send<void>('POST', '/api/sign-out')
export const getStatus = () => send<Status>('GET', '/api/status')
export const clockIn = () => send<ClockResult>('POST', '/api/clock-in')
export const clockOut = () => send<ClockResult>('POST', '/api/clock-out')
export const forgotClockOut = () => send<void>('POST', '/api/forgot-clock-out')
export const getShiftMonths = () => send<MonthTotal[]>('GET', '/api/shifts/months')
export const getShifts = (year: number, month: number) => send<Shift[]>('GET', `/api/shifts?year=${year}&month=${month}`)
