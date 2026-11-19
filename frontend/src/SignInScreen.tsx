import { useState, type SubmitEvent } from 'react'
import { signIn, StatusCode, type Employee } from './api'
import { messages } from './messages'

type Props = {
  message: string
  onSignedIn: (employee: Employee) => void
}

function signInError(status: number): string {
  if (status === StatusCode.Unauthorized) return messages.wrongNumberOrPin
  if (status === StatusCode.Forbidden) return messages.employeeInactive
  return messages.somethingWentWrong
}

export function SignInScreen({ message, onSignedIn }: Props) {
  const [employeeNumber, setEmployeeNumber] = useState('')
  const [pin, setPin] = useState('')
  const [hasLeftPin, setHasLeftPin] = useState(false)
  const [error, setError] = useState('')
  const [isSending, setIsSending] = useState(false)

  const isEmployeeNumberValid = /^\d+$/.test(employeeNumber)
  const isPinValid = /^\d{4}$/.test(pin)
  const isPinStillBeingTyped = /^\d{1,3}$/.test(pin) && !hasLeftPin
  const showEmployeeNumberError = employeeNumber !== '' && !isEmployeeNumberValid
  const showPinError = pin !== '' && !isPinValid && !isPinStillBeingTyped

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSending(true)
    const response = await signIn(employeeNumber, pin)
    setIsSending(false)
    if (response.data) {
      onSignedIn(response.data)
      return
    }
    setPin('')
    setError(signInError(response.status))
  }

  return (
    <main className="screen">
      <h1>Shift Clock</h1>
      <p className={error ? 'message error' : 'message'} role="status">
        {error || message}
      </p>
      <form onSubmit={handleSubmit}>
        <label>
          Employee number
          <input
            inputMode="numeric"
            autoComplete="off"
            value={employeeNumber}
            onChange={(event) => {
              setEmployeeNumber(event.target.value)
              setError('')
            }}
          />
          {showEmployeeNumberError && <span className="error">{messages.employeeNumberNotDigits}</span>}
        </label>
        <label>
          PIN
          <input
            type="password"
            inputMode="numeric"
            autoComplete="off"
            value={pin}
            onChange={(event) => {
              setPin(event.target.value)
              setError('')
            }}
            onFocus={() => setHasLeftPin(false)}
            onBlur={() => setHasLeftPin(true)}
          />
          {showPinError && <span className="error">{messages.pinNotFourDigits}</span>}
        </label>
        <button className="primary" type="submit" disabled={!isEmployeeNumberValid || !isPinValid || isSending}>
          Sign in
        </button>
      </form>
    </main>
  )
}
