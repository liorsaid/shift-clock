import { useCallback, useEffect, useState } from 'react'
import { signOut, StatusCode, type Employee } from './api'
import { ClockTab } from './ClockTab'
import { messages } from './messages'
import { MyShiftsTab } from './MyShiftsTab'

type Props = {
  employee: Employee
  onSignedOut: (message: string) => void
}

const IdleSignOutDelayMs = 30_000
const ActivityEvents = ['pointerdown', 'keydown']
const ConfirmationDelayMs = 3_000

export function SignedInScreen({ employee, onSignedOut }: Props) {
  const [tab, setTab] = useState<'clock' | 'myShifts'>('clock')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState('')
  const [isSigningOut, setIsSigningOut] = useState(false)

  const signOutOrStaySignedIn = useCallback(
    async (signedOutMessage: string) => {
      setError('')
      setIsSigningOut(true)
      const response = await signOut()
      setIsSigningOut(false)
      if (response.status === StatusCode.NoContent) onSignedOut(signedOutMessage)
      else setError(messages.somethingWentWrong)
    },
    [onSignedOut],
  )

  useEffect(() => {
    let idleTimer = 0
    function restartIdleTimer() {
      clearTimeout(idleTimer)
      idleTimer = setTimeout(() => {
        restartIdleTimer()
        signOutOrStaySignedIn(messages.signedOutWhenIdle)
      }, IdleSignOutDelayMs)
    }
    restartIdleTimer()
    ActivityEvents.forEach((event) => window.addEventListener(event, restartIdleTimer))
    return () => {
      clearTimeout(idleTimer)
      ActivityEvents.forEach((event) => window.removeEventListener(event, restartIdleTimer))
    }
  }, [signOutOrStaySignedIn])

  useEffect(() => {
    if (!confirmation) return
    const signOutTimer = setTimeout(() => onSignedOut(''), ConfirmationDelayMs)
    return () => clearTimeout(signOutTimer)
  }, [confirmation, onSignedOut])

  if (confirmation) {
    return (
      <main className="screen">
        <p className="status">{confirmation}</p>
        <p>{messages.signingOut}</p>
      </main>
    )
  }

  return (
    <main className="screen">
      <header className="signed-in">
        <span>Signed in as {employee.displayName}</span>
        <button disabled={isSigningOut} onClick={() => signOutOrStaySignedIn(messages.signedOut)}>
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
      {tab === 'clock' && <ClockTab onSignedOut={onSignedOut} onClocked={setConfirmation} />}
      {tab === 'myShifts' && <MyShiftsTab onSignedOut={onSignedOut} />}
    </main>
  )
}
