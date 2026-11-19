import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// index.css is the leftover template base; import it first so the design
// tokens below win the cascade (e.g. --bg, body background/color) and the
// template's light/dark overrides don't reintroduce a dark background.
import './index.css'
import './styles/tokens.css'
import './components/ui/ui.css'
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
