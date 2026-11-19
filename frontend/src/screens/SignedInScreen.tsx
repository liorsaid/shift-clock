import { useCallback, useEffect, useState } from 'react'
import { signOut, StatusCode, type Employee } from '../shared/api'
import { messages } from '../shared/messages'
import { ClockTab } from './ClockTab'
import { MyShiftsTab } from './MyShiftsTab'

type Props = {
  employee: Employee
  onSignedOut: (message: string) => void
}

const IdleSignOutDelayMs = 30_000
const ActivityEvents = ['pointerdown', 'keydown']
const ConfirmationDelayMs = 3_000

type Confirmation = { message: string; note?: string }

export function SignedInScreen({ employee, onSignedOut }: Props) {
  const [tab, setTab] = useState<'clock' | 'myShifts'>('clock')
  const [confirmation, setConfirmation] = useState<Confirmation>()
  const [error, setError] = useState('')
  const [isSigningOut, setIsSigningOut] = useState(false)

  const signOutOrStaySignedIn = useCallback(async (afterSignOut: () => void) => {
    setError('')
    setIsSigningOut(true)
    const response = await signOut()
    setIsSigningOut(false)
    if (response.status === StatusCode.NoContent) afterSignOut()
    else setError(messages.somethingWentWrong)
  }, [])

  useEffect(() => {
    let idleTimer = 0
    function restartIdleTimer() {
      clearTimeout(idleTimer)
      idleTimer = setTimeout(() => {
        restartIdleTimer()
        signOutOrStaySignedIn(() => onSignedOut(messages.signedOutWhenIdle))
      }, IdleSignOutDelayMs)
    }
    restartIdleTimer()
    ActivityEvents.forEach((event) => window.addEventListener(event, restartIdleTimer))
    return () => {
      clearTimeout(idleTimer)
      ActivityEvents.forEach((event) => window.removeEventListener(event, restartIdleTimer))
    }
  }, [signOutOrStaySignedIn, onSignedOut])

  useEffect(() => {
    if (!confirmation) return
    const signOutTimer = setTimeout(() => onSignedOut(''), ConfirmationDelayMs)
    return () => clearTimeout(signOutTimer)
  }, [confirmation, onSignedOut])

  if (confirmation) {
    return (
      <main className="screen">
        <p className="status">{confirmation.message}</p>
        {confirmation.note && <p>{confirmation.note}</p>}
      </main>
    )
  }

  return (
    <main className="screen">
      <header className="signed-in">
        <span>Signed in as {employee.displayName}</span>
        <button disabled={isSigningOut} onClick={() => signOutOrStaySignedIn(() => setConfirmation({ message: messages.signedOut }))}>
          Sign out
        </button>
      </header>
      <p className="message error" role="status">
        {error}
      </p>
      <nav className="tabs">
        <button className={tab === 'clock' ? 'active' : ''} onClick={() => setTab('clock')}>
          Clock
        </button>
        <button className={tab === 'myShifts' ? 'active' : ''} onClick={() => setTab('myShifts')}>
          My shifts
        </button>
      </nav>
      {tab === 'clock' && <ClockTab onSignedOut={onSignedOut} onClocked={(message) => setConfirmation({ message, note: messages.signingOut })} />}
      {tab === 'myShifts' && <MyShiftsTab onSignedOut={onSignedOut} />}
    </main>
  )
}
