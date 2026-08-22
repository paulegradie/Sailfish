import { useRouter } from 'next/router'

import { Callout } from '@/components/Callout'
import { QuickLink, QuickLinks } from '@/components/QuickLinks'
import { Terminal } from '@/components/Terminal'

// Plain <img> elements don't get Next's basePath applied (the production site
// deploys under /Sailfish), so root-relative srcs must be prefixed at render time.
function Figure({ src, alt = '', caption }) {
  const { basePath } = useRouter()
  const resolvedSrc = src.startsWith('/') ? `${basePath}${src}` : src
  return (
    <figure>
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={resolvedSrc} alt={alt} />
      <figcaption>{caption}</figcaption>
    </figure>
  )
}

const tags = {
  terminal: {
    attributes: {
      title: { type: String },
    },
    render: Terminal,
  },
  callout: {
    attributes: {
      title: { type: String },
      type: {
        type: String,
        default: 'note',
        matches: ['note', 'warning', 'success'],
        errorLevel: 'critical',
      },
    },
    render: Callout,
  },
  figure: {
    selfClosing: true,
    attributes: {
      src: { type: String },
      alt: { type: String },
      caption: { type: String },
    },
    render: Figure,
  },
  'quick-links': {
    render: QuickLinks,
  },
  'quick-link': {
    selfClosing: true,
    render: QuickLink,
    attributes: {
      title: { type: String },
      description: { type: String },
      icon: { type: String },
      href: { type: String },
    },
  },
}

export default tags
