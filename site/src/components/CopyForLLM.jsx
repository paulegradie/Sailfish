import { useCallback, useEffect, useRef, useState } from 'react'
import { useRouter } from 'next/router'
import clsx from 'clsx'

// A "Copy for LLM" control: copies the entire Sailfish documentation, as one LLM-ready
// markdown document, to the clipboard so it can be pasted into an agent's context. The
// spec is generated at build time (scripts/generate-llms-spec.mjs) and served from
// public/llms-full.txt; we prefetch it so the click handler can write synchronously
// within the user gesture (the most reliable path across browsers).
export function CopyForLLM() {
  const { basePath } = useRouter()
  const specUrl = `${basePath}/llms-full.txt`
  const specRef = useRef(null)
  const [state, setState] = useState('idle') // 'idle' | 'copied' | 'error'
  // Whether the spec is prefetched and cached. The copy path only runs once this is true, so the
  // clipboard write never has to await the network mid-gesture (see onCopy).
  const [ready, setReady] = useState(false)

  // Fetch the spec and cache it. fetch() resolves even on 4xx/5xx, so guard on r.ok — otherwise an
  // error page body would be cached and later copied as if it were the spec.
  const loadSpec = useCallback(async () => {
    const r = await fetch(specUrl)
    if (!r.ok) throw new Error(`HTTP ${r.status}`)
    const text = await r.text()
    specRef.current = text
    return text
  }, [specUrl])

  useEffect(() => {
    let cancelled = false
    loadSpec()
      .then(() => {
        if (!cancelled) setReady(true)
      })
      .catch(() => {
        /* left un-ready; the first click will load it (without copying) then re-arm */
      })
    return () => {
      cancelled = true
    }
  }, [loadSpec])

  const writeToClipboard = useCallback(async (text) => {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
      return
    }
    // Fallback for non-secure contexts / older browsers.
    const ta = document.createElement('textarea')
    ta.value = text
    ta.style.position = 'fixed'
    ta.style.opacity = '0'
    document.body.appendChild(ta)
    ta.select()
    let ok = false
    try {
      // execCommand returns false when the copy was unsupported or rejected; treat that as a failure
      // rather than silently reporting success.
      ok = document.execCommand('copy')
    } finally {
      document.body.removeChild(ta)
    }
    if (!ok) throw new Error('Clipboard copy was rejected by the browser')
  }, [])

  const onCopy = useCallback(async () => {
    // If the spec isn't cached yet (prefetch still in flight or it failed), load it now WITHOUT
    // touching the clipboard. Awaiting the network before the copy would let the click's transient
    // user activation expire and the browser would block writeText/execCommand. The user copies on
    // the next click, when the write runs synchronously from cache inside a fresh activation.
    if (specRef.current == null) {
      try {
        await loadSpec()
        setReady(true)
        setState('idle')
      } catch {
        setState('error')
        setTimeout(() => setState('idle'), 3000)
      }
      return
    }

    try {
      await writeToClipboard(specRef.current)
      setState('copied')
      setTimeout(() => setState('idle'), 2000)
    } catch {
      setState('error')
      setTimeout(() => setState('idle'), 3000)
    }
  }, [loadSpec, writeToClipboard])

  const label =
    state === 'copied'
      ? 'Copied!'
      : state === 'error'
      ? 'Copy failed'
      : ready
      ? 'Copy for LLM'
      : 'Preparing…'

  return (
    <div className="not-prose flex items-center gap-2">
      <button
        type="button"
        onClick={onCopy}
        title="Copy the entire Sailfish documentation as one LLM-ready document"
        className={clsx(
          'inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-sm font-medium transition',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500/60',
          state === 'copied'
            ? 'border-green-500/40 bg-green-500/10 text-green-700 dark:text-green-300'
            : state === 'error'
            ? 'border-red-500/40 bg-red-500/10 text-red-700 dark:text-red-300'
            : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 hover:text-slate-900 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white'
        )}
        aria-live="polite"
      >
        {state === 'copied' ? (
          <CheckIcon className="h-4 w-4" />
        ) : (
          <CopyIcon className="h-4 w-4" />
        )}
        {label}
      </button>
      <a
        href={specUrl}
        target="_blank"
        rel="noreferrer"
        title="View the raw LLM spec (llms-full.txt)"
        className="text-xs font-medium text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
      >
        view raw
      </a>
    </div>
  )
}

function CopyIcon(props) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}>
      <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
    </svg>
  )
}

function CheckIcon(props) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}>
      <path d="M20 6 9 17l-5-5" />
    </svg>
  )
}
