import { useCallback, useEffect, useState } from 'react'
import { SignedInScreen } from './screens/SignedInScreen'
import { SignInScreen } from './screens/SignInScreen'
import { getMe, type Employee } from './shared/api'

function App() {
  const [isCheckingSession, setIsCheckingSession] = useState(true)
  const [employee, setEmployee] = useState<Employee>()
  const [message, setMessage] = useState('')

  useEffect(() => {
    getMe().then((response) => {
      setEmployee(response.data)
      setIsCheckingSession(false)
    })
  }, [])

  const showSignIn = useCallback((nextMessage: string) => {
    setEmployee(undefined)
    setMessage(nextMessage)
  }, [])

  if (isCheckingSession) return null
  if (employee) return <SignedInScreen employee={employee} onSignedOut={showSignIn} />
  return <SignInScreen message={message} onSignedIn={setEmployee} />
}

export default App
