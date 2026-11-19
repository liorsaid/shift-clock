import { useCallback, useEffect, useState } from 'react'
import { clockIn, clockOut, forgotClockOut, getStatus, StatusCode, type ApiResponse, type Status } from '../shared/api'
import { messages } from '../shared/messages'
import { formatZurichTime } from '../shared/zurichTime'

type Props = {
  onSignedOut: (message: string) => void
  onClocked: (confirmation: string) => void
}

export function ClockTab({ onSignedOut, onClocked }: Props) {
  const [status, setStatus] = useState<Status>()
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [isConfirmingForgot, setIsConfirmingForgot] = useState(false)
  const isOnShift = Boolean(status?.onShiftSince)

  const showStatus = useCallback(
    (response: ApiResponse<Status>) => {
      if (response.data) setStatus(response.data)
      else if (response.status === StatusCode.Unauthorized) onSignedOut(messages.sessionEnded)
      else setError(messages.somethingWentWrong)
    },
    [onSignedOut],
  )

  useEffect(() => {
    getStatus().then(showStatus)
  }, [showStatus])

  function loadStatus() {
    getStatus().then(showStatus)
  }

  function startRequest() {
    setMessage('')
    setError('')
    setIsSending(true)
  }

  function showFailure(responseStatus: number, conflictMessage: string) {
    if (responseStatus === StatusCode.Unauthorized) {
      onSignedOut(messages.sessionEnded)
    } else if (responseStatus === StatusCode.Conflict) {
      setError(conflictMessage)
      loadStatus()
    } else if (responseStatus === StatusCode.ServiceUnavailable) {
      setError(messages.timeServiceUnavailable)
    } else {
      setError(messages.somethingWentWrong)
    }
  }

  async function handleClock() {
    startRequest()
    const response = isOnShift ? await clockOut() : await clockIn()
    setIsSending(false)
    if (!response.data) {
      showFailure(response.status, isOnShift ? messages.notClockedIn : messages.alreadyClockedIn)
      return
    }
    const { clockInAt, clockOutAt } = response.data
    onClocked(
      clockOutAt
        ? messages.clockedOut(formatZurichTime(clockOutAt))
        : messages.clockedIn(formatZurichTime(clockInAt)),
    )
  }

  async function handleForgotClockOut() {
    setIsConfirmingForgot(false)
    startRequest()
    const response = await forgotClockOut()
    setIsSending(false)
    if (response.status !== StatusCode.NoContent) {
      showFailure(response.status, messages.notClockedIn)
      return
    }
    setMessage(messages.markedMissingClockOut)
    loadStatus()
  }

  return (
    <section className="clock">
      {status && (
        <p className="status">
          {status.onShiftSince ? messages.onShiftSince(formatZurichTime(status.onShiftSince)) : messages.offShift}
        </p>
      )}
      {status && !isConfirmingForgot && (
        <button className="primary" disabled={isSending} onClick={handleClock}>
          {isOnShift ? 'Clock Out' : 'Clock In'}
        </button>
      )}
      {isOnShift && !isConfirmingForgot && (
        <button className="link" disabled={isSending} onClick={() => setIsConfirmingForgot(true)}>
          I forgot to clock out earlier
        </button>
      )}
      {isConfirmingForgot && (
        <>
          <p>{messages.confirmForgotClockOut}</p>
          <button className="primary" disabled={isSending} onClick={handleForgotClockOut}>
            Yes, mark it
          </button>
          <button onClick={() => setIsConfirmingForgot(false)}>Cancel</button>
        </>
      )}
      <p className={error ? 'message error' : 'message'} role="status">
        {error || message}
      </p>
    </section>
  )
}
