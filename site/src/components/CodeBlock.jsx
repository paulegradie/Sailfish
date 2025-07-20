import { useState } from 'react'
import clsx from 'clsx'
import Highlight, { Prism, defaultProps } from "prism-react-renderer";
import {
  ClipboardIcon,
  ClipboardDocumentCheckIcon,
  PlayIcon,
  CodeBracketIcon
} from '@heroicons/react/24/outline'
import { trackCodeCopy } from '@/components/ui/Analytics'

(typeof global !== "undefined" ? global : window).Prism = Prism
await import("prismjs/components/prism-applescript")
require("prismjs/components/prism-csharp");

// Custom theme for better contrast and readability
const customTheme = {
  plain: {
    color: "#e2e8f0", // slate-200
    backgroundColor: "#0f172a", // slate-900
  },
  styles: [
    {
      types: ["comment", "prolog", "doctype", "cdata"],
      style: {
        color: "#64748b", // slate-500
        fontStyle: "italic",
      },
    },
    {
      types: ["namespace"],
      style: {
        opacity: 0.7,
      },
    },
    {
      types: ["string", "attr-value"],
      style: {
        color: "#22d3ee", // cyan-400
      },
    },
    {
      types: ["punctuation", "operator"],
      style: {
        color: "#94a3b8", // slate-400
      },
    },
    {
      types: ["entity", "url", "symbol", "number", "boolean", "variable", "constant", "property", "regex", "inserted"],
      style: {
        color: "#fbbf24", // amber-400
      },
    },
    {
      types: ["atrule", "keyword", "attr-name", "selector"],
      style: {
        color: "#a78bfa", // violet-400
      },
    },
    {
      types: ["function", "deleted", "tag"],
      style: {
        color: "#fb7185", // rose-400
      },
    },
    {
      types: ["function-variable"],
      style: {
        color: "#60a5fa", // blue-400
      },
    },
    {
      types: ["tag", "selector", "keyword"],
      style: {
        color: "#34d399", // emerald-400
      },
    },
  ],
}

function CopyButton({ code, language }) {
  const [copied, setCopied] = useState(false)

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)

      // Track copy analytics
      trackCodeCopy(language || 'unknown', code.length)
    } catch (err) {
      console.error('Failed to copy code:', err)
    }
  }

  return (
    <button
      onClick={copyToClipboard}
      className="flex items-center gap-2 rounded-md bg-slate-700/50 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:bg-slate-600 hover:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-slate-900"
      aria-label={copied ? 'Copied!' : 'Copy code'}
    >
      {copied ? (
        <>
          <ClipboardDocumentCheckIcon className="h-4 w-4" />
          <span>Copied!</span>
        </>
      ) : (
        <>
          <ClipboardIcon className="h-4 w-4" />
          <span>Copy</span>
        </>
      )}
    </button>
  )
}

// Better filename mapping for different languages
const getDefaultFilename = (language) => {
  const filenameMap = {
    'csharp': 'Program.cs',
    'cs': 'Program.cs',
    'javascript': 'script.js',
    'js': 'script.js',
    'typescript': 'script.ts',
    'ts': 'script.ts',
    'python': 'script.py',
    'py': 'script.py',
    'java': 'Main.java',
    'json': 'config.json',
    'yaml': 'config.yaml',
    'yml': 'config.yml',
    'xml': 'config.xml',
    'html': 'index.html',
    'css': 'styles.css',
    'scss': 'styles.scss',
    'bash': 'script.sh',
    'shell': 'script.sh',
    'sql': 'query.sql',
    'dockerfile': 'Dockerfile',
    'text': 'file.txt'
  }
  return filenameMap[language?.toLowerCase()] || `code.${language || 'txt'}`
}

function CodeHeader({ language, filename, showCopy = true, code }) {
  return (
    <div className="flex items-center justify-between border-b border-slate-700/50 bg-slate-800/30 px-4 py-3">
      <div className="flex items-center gap-3">
        <div className="flex gap-1.5">
          <div className="h-3 w-3 rounded-full bg-red-500" />
          <div className="h-3 w-3 rounded-full bg-yellow-500" />
          <div className="h-3 w-3 rounded-full bg-green-500" />
        </div>
        <div className="flex items-center gap-2 text-sm text-slate-300">
          <CodeBracketIcon className="h-4 w-4" />
          <span>{filename || getDefaultFilename(language)}</span>
        </div>
      </div>
      {showCopy && <CopyButton code={code} language={language} />}
    </div>
  )
}

export default function CodeBlock({
  language = 'text',
  code,
  filename,
  showLineNumbers = true,
  showCopy = true,
  maxHeight,
  className
}) {
  const cleanCode = code.trim()

  return (
    <div className={clsx(
      'group relative overflow-hidden rounded-xl bg-slate-900 shadow-lg',
      'transition-all duration-200 hover:shadow-xl',
      className
    )}>
      <CodeHeader
        language={language}
        filename={filename}
        showCopy={showCopy}
        code={cleanCode}
      />

      <div className={clsx(
        'overflow-x-auto',
        maxHeight && `max-h-${maxHeight} overflow-y-auto`
      )}>
        <Highlight
          {...defaultProps}
          code={cleanCode}
          language={language}
          theme={customTheme}
        >
          {({
            className: highlightClassName,
            style,
            tokens,
            getLineProps,
            getTokenProps,
          }) => (
            <pre
              className={clsx(
                highlightClassName,
                'flex text-sm leading-relaxed'
              )}
              style={style}
            >
              {showLineNumbers && (
                <div className="select-none border-r border-slate-700/50 bg-slate-800/20 px-4 py-4 text-right text-slate-400">
                  {tokens.map((_, index) => (
                    <div key={index} className="leading-relaxed">
                      {index + 1}
                    </div>
                  ))}
                </div>
              )}
              <code className="flex-1 px-4 py-4">
                {tokens.map((line, lineIndex) => (
                  <div key={lineIndex} {...getLineProps({ line })}>
                    {line.map((token, tokenIndex) => (
                      <span
                        key={tokenIndex}
                        {...getTokenProps({ token })}
                      />
                    ))}
                  </div>
                ))}
              </code>
            </pre>
          )}
        </Highlight>
      </div>
    </div>
  )
}