import { useCallback, useEffect, useState } from 'react'
import { getShiftMonths, getShifts, StatusCode, type MonthTotal, type Shift } from './api'
import { messages } from './messages'
import { formatDuration, formatMonth, formatZurichTime } from './zurichTime'

type Props = {
  onSignedOut: (message: string) => void
}

function shiftEnd(shift: Shift): string {
  if (shift.clockOutAt) return formatZurichTime(shift.clockOutAt)
  if (shift.isMissingClockOut) return messages.missingClockOutInHistory
  return messages.stillOnShift
}

export function MyShiftsTab({ onSignedOut }: Props) {
  const [months, setMonths] = useState<MonthTotal[]>()
  const [monthIndex, setMonthIndex] = useState(0)
  const [shifts, setShifts] = useState<Shift[]>([])
  const [error, setError] = useState('')
  const month = months?.[monthIndex]

  const showFailure = useCallback(
    (status: number) => {
      if (status === StatusCode.Unauthorized) onSignedOut(messages.sessionEnded)
      else setError(messages.somethingWentWrong)
    },
    [onSignedOut],
  )

  useEffect(() => {
    getShiftMonths().then((response) => {
      if (response.data) setMonths(response.data)
      else showFailure(response.status)
    })
  }, [showFailure])

  useEffect(() => {
    if (!month) return
    let isShownMonth = true
    getShifts(month.year, month.month).then((response) => {
      if (!isShownMonth) return
      if (response.data) setShifts(response.data)
      else showFailure(response.status)
    })
    return () => {
      isShownMonth = false
    }
  }, [month, showFailure])

  function showMonth(index: number) {
    setShifts([])
    setMonthIndex(index)
  }

  if (error) return <p className="message error" role="status">{error}</p>
  if (!months) return null
  if (!month) return <p>{messages.noShiftsYet}</p>

  return (
    <section className="my-shifts">
      <div className="month">
        <button disabled={monthIndex === months.length - 1} onClick={() => showMonth(monthIndex + 1)}>
          ‹
        </button>
        <span className="status">{formatMonth(month.year, month.month)}</span>
        <button disabled={monthIndex === 0} onClick={() => showMonth(monthIndex - 1)}>
          ›
        </button>
      </div>
      <p>{messages.monthTotal(formatDuration(month.totalSeconds))}</p>
      <ul>
        {shifts.map((shift) => (
          <li key={shift.clockInAt}>
            {formatZurichTime(shift.clockInAt)} → {shiftEnd(shift)}
          </li>
        ))}
      </ul>
    </section>
  )
}
