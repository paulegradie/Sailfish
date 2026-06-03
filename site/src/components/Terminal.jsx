import clsx from 'clsx'

// A terminal-window chrome used to present Sailfish console output beautifully.
// Wraps a fenced code block: the fence supplies the monospace, pre-formatted body
// (preserving the box-drawing / block glyphs in distribution plots) while this
// component paints the window frame, the traffic-light dots, and an optional title.
//
// Usage in Markdoc:
//   {% terminal title="ConcatBenchmarks ▸ dotnet test" %}
//   ```
//   ...console output...
//   ```
//   {% /terminal %}
export function Terminal({ title, children }) {
  return (
    <div className="not-prose my-8 overflow-hidden rounded-xl bg-slate-900 shadow-xl ring-1 ring-slate-300/10 dark:bg-slate-800/40">
      <div className="flex items-center gap-2 border-b border-white/5 bg-slate-800/80 px-4 py-2.5">
        <span className="h-3 w-3 rounded-full bg-red-500/90" />
        <span className="h-3 w-3 rounded-full bg-amber-400/90" />
        <span className="h-3 w-3 rounded-full bg-green-500/90" />
        {title && (
          <span className="ml-3 truncate font-mono text-xs font-medium tracking-wide text-slate-400">
            {title}
          </span>
        )}
      </div>
      <div className="overflow-x-auto text-[13px] leading-relaxed [&_pre]:!my-0 [&_pre]:!rounded-none [&_pre]:!bg-transparent [&_pre]:!shadow-none [&_pre]:!ring-0">
        {children}
      </div>
    </div>
  )
}
